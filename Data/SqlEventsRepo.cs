using FinalProject.Models;
using FinalProject.Repository;
using Microsoft.AspNetCore.Http;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace FinalProject.Data
{
    
    public class SqlEventsRepo : EventRepoInterface
    {
        private const string KEY = "&key=Your_Key";
        private readonly DatabaseContext _context;
        public SqlEventsRepo(DatabaseContext context)
        {
            _context = context;
        }

        public List<Event> GetAllEvents()
        {
            return _context.Events.ToList();
        }

        public Event GetEventById(int id)
        {
            return _context.Events.FirstOrDefault(p => p.eventId == id);
        }

        public void CreateEvent(Event eve)
        {
            if(eve == null)
            {
                throw new ArgumentNullException(nameof(eve));
            }
            _context.Events.Add(eve);
        }

        public void UpdateEvent(int id, Event newEvent)
        {
            var entity = _context.Events.FirstOrDefault(x => x.eventId == id);

            if (entity != null)
            {
               
                // Make changes on entity
                entity.eventName = newEvent.eventName;
                entity.eventDescription = newEvent.eventDescription;
                entity.eventDate = newEvent.eventDate;
                entity.streetLocation = newEvent.streetLocation;

                /* If the entry is being tracked, then invoking update API is not needed. 
                  The API only needs to be invoked if the entry was not tracked. 
                  https://www.learnentityframeworkcore.com/dbcontext/modifying-data */
                _context.Events.Update(entity);

                // Save changes in database
                _context.SaveChanges();
            }
        }

        public void DeleteEvent(Event eve)
        {
            if (eve == null)
            {
                throw new ArgumentNullException(nameof(eve));
            }
            _context.Events.Remove(eve);
        }


        // ATTEND EVENTS
        public Event GetEventAttendants(int ToFindEventId)
        {
            var foundEvent = _context.Events.FirstOrDefault(x => x.eventId == ToFindEventId);
            foundEvent.participants = new List<Person>();
            var checkAttendants = _context.AttendEvent.ToList();

            foreach (var person in checkAttendants)
            {
                if (person.eventId == ToFindEventId)
                {
                    var personFound = _context.Persons.FirstOrDefault(p => p.personId == person.personId);
                    foundEvent.participants.Add(personFound);
                }
            }
            return foundEvent;
        }

        public void AddPersonToEvent(int eventoId, int participantId)
        {
            var entity = new AttendEvent();
            entity.personId = participantId;
            entity.eventId = eventoId;
            entity.Person = null;
            entity.Event = null;
            _context.AttendEvent.Add(entity);

            _context.SaveChanges();
        }

        public void RemovePersonFromEvent(int eventoId, int participantId)
        {
            var checkAttendants = _context.AttendEvent.ToList();
            foreach (var person in checkAttendants)
            {
                if (person.eventId == eventoId && person.personId == participantId)
                {
                    _context.AttendEvent.Remove(person);
                }
            }
            _context.SaveChanges();
        }

        public List<Person> GetAllPeeps()
        {
            return _context.Persons.ToList();
        }

        public Person GetPersonById(int id)
        {
            var foundPerson = _context.Persons.FirstOrDefault(p => p.personId == id);
            if(foundPerson == null)
            {
                return null;
            }

            foundPerson.ParticipatingInEvents = new List<Event>();
            var checkAttendants = _context.AttendEvent.ToList();
            foreach (var attendingEvent in checkAttendants)
            {
                if (attendingEvent.personId == id)
                {
                    var eventFound = _context.Events.FirstOrDefault(p => p.eventId == attendingEvent.eventId);
                    foundPerson.ParticipatingInEvents.Add(eventFound);
                }
            }
            return foundPerson;
        }

        public void CreatePerson(Person newPerson)
        {
            if (newPerson == null)
            {
                throw new ArgumentNullException(nameof(newPerson));
            }
            _context.Persons.Add(newPerson);
        }

        public void UpdatePerson(int id, Person newPerson)
        {
            var entity = _context.Persons.FirstOrDefault(x => x.personId == id);

            if (entity != null)
            {
                // Make changes on entity
                entity.name = newPerson.name;
                entity.lastName= newPerson.lastName;
                entity.phoneNumber = newPerson.phoneNumber;

                _context.Persons.Update(entity);

                // Save changes in database
                _context.SaveChanges();
            }
        }

        public void DeletePerson(Person delPerson)
        {
            if (delPerson == null)
            {
                throw new ArgumentNullException(nameof(delPerson));
            }
            _context.Persons.Remove(delPerson);
        }

        public bool SaveChanges()
        {
            return (_context.SaveChanges() >= 0);
        }
          public Event GetRequestEvent(Event dataa)
        {
            string url = "https://maps.googleapis.com/maps/api/place/textsearch/json?query=" + dataa.streetLocation + KEY;
            string lat = null;
            string lng = null;       
            string urlData = String.Empty;
            WebClient wc = new WebClient();
            urlData = wc.DownloadString(url);
            string[] data = urlData.Split();
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].Contains("lat"))
                {
                    lat = data[i + 2];
                    break;
                }
            }
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].Contains("lng"))
                {
                    lng = data[i + 2];
                    break;
                }
            }
            dataa.mapLocation = "https://www.google.com/maps/search/?api=1&query=" + lat + lng;
            return dataa;
        }

        //Add links for one event
        public Event AddLinks(Event addLinksEvent, HttpRequest path)
        {
            addLinksEvent.links = new List<Link>();
            for (int i = 0; i <= 2; i++)
            {
                Link newLink = new Link();
                newLink.Href = "https://localhost:44348" + path.Path;
                if(i == 0)
                {
                    newLink.Method = "GET";
                    newLink.Rel = "SELF";
                }
                if (i == 1)
                {
                    newLink.Method = "PUT";
                    newLink.Rel = "Update_Event";
                }
                if (i == 2)
                {
                    newLink.Method = "DELETE";
                    newLink.Rel = "Delete_Event";
                }

                addLinksEvent.links.Add(newLink);
            }
            return addLinksEvent;
        }
        //Add links for multiple events
        public Event AddLinksMultiple(Event addLinksEvent,HttpRequest path)
        {
            addLinksEvent.links = new List<Link>();
            for (int i = 0; i <= 2; i++)
            {
                Link newLink = new Link();
                newLink.Href = "https://localhost:44348" + path.Path+"/"+addLinksEvent.eventId;
                if (i == 0)
                {
                    newLink.Method = "GET";
                    newLink.Rel = "SELF";
                }
                if (i == 1)
                {
                    newLink.Method = "PUT";
                    newLink.Rel = "Update_Event";
                }
                if (i == 2)
                {
                    newLink.Method = "DELETE";
                    newLink.Rel = "Delete_Event";
                }
                addLinksEvent.links.Add(newLink);
            }
            return addLinksEvent;
        }
        //add links for one person
        public Person AddLinksPer(Person addLinksEvent, HttpRequest path)
        {
            addLinksEvent.links = new List<Link>();
            for (int i = 0; i <= 2; i++)
            {
                Link newLink = new Link();
                newLink.Href = "https://localhost:44348" + path.Path;
                if (i == 0)
                {
                    newLink.Method = "GET";
                    newLink.Rel = "SELF";
                }
                if (i == 1)
                {
                    newLink.Method = "PUT";
                    newLink.Rel = "Update_Person";
                }
                if (i == 2)
                {
                    newLink.Method = "DELETE";
                    newLink.Rel = "Delete_Person";
                }
                addLinksEvent.links.Add(newLink);
            }
            return addLinksEvent;
        }
        //add links for multiple people
        public Person AddLinksPerson(Person addLinksEvent, HttpRequest path)
        {
            addLinksEvent.links = new List<Link>();
            for (int i = 0; i <= 2; i++)
            {
                Link newLink = new Link();
                newLink.Href = "https://localhost:44348" + path.Path+"/"+addLinksEvent.personId;
                if (i == 0)
                {
                    newLink.Method = "GET";
                    newLink.Rel = "SELF";
                }
                if (i == 1)
                {
                    newLink.Method = "PUT";
                    newLink.Rel = "Update_Person";
                }
                if (i == 2)
                {
                    newLink.Method = "DELETE";
                    newLink.Rel = "Delete_Person";
                }
                addLinksEvent.links.Add(newLink);
            }
            return addLinksEvent;
        }


    }
}
