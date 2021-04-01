using FinalProject.Controllers;
using FinalProject.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinalProject.Repository
{
    public interface EventRepoInterface
    {
        bool SaveChanges();
        List<Event> GetAllEvents();
        Event GetEventById(int id);
        void CreateEvent(Event newEvent);
        void UpdateEvent(int id, Event newEvent);
        void DeleteEvent(Event newEvent);

        // attendEvent
        Event GetEventAttendants(int id);
        void AddPersonToEvent(int id, int id2);
        void RemovePersonFromEvent(int id, int id2);
        Event GetRequestEvent(Event data);

        // People
        List<Person> GetAllPeeps();
        Person GetPersonById(int id);
        void CreatePerson(Person newPerson);
        void UpdatePerson(int id, Person newPerson);
        void DeletePerson(Person delPerson);

        //Add links
        Event AddLinks(Event addLinksEvent, HttpRequest path);
        Event AddLinksMultiple(Event addLinksEvent, HttpRequest path);
        Person AddLinksPer(Person addLinksEvent, HttpRequest path);
        Person AddLinksPerson(Person addLinksEvent, HttpRequest path);

    }
}
