using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinalProject.Data;
using FinalProject.Models;
using FinalProject.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RiskFirst.Hateoas;
using RiskFirst.Hateoas.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FinalProject.Controllers
{
    [Route("api/events")]
    [ApiController]
    [Produces("application/json")]
    public class EventsController : ControllerBase
    {
        private readonly EventRepoInterface _repository;

        private readonly IMemoryCache _cache;
        private readonly ILinksService _linkService;

        public EventsController(EventRepoInterface repository, IMemoryCache memoryCache, ILinksService linksService)
        {
            _repository = repository;
            _cache = memoryCache;
            _linkService = linksService;
        }

        /// <summary>
        /// Gets the list of all Events
        /// </summary>
        /// <returns>The list of Events</returns>
        // GET: api/events
        [HttpGet(Name = "GetAllEvents")]
        public ActionResult<IEnumerable<Event>> GetAllEvents()
        {
            var allEvents = new List<Event>(); 
            List<Event> data = new List<Event>();
            if(!_cache.TryGetValue("AllEventsList", out allEvents))
            {
                Console.WriteLine("No cache found, creating new");

                allEvents = _repository.GetAllEvents();
                var callPath = Request;
                foreach (var a in allEvents)
                {
                  data.Add(_repository.GetRequestEvent(a));
                    _repository.AddLinksMultiple(a, callPath);
                }
                
                _cache.Set("AllEventsList", allEvents, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(10)));
            }
            else
            {
                Console.WriteLine("Cache found, pulling out");
            }
            return Ok(data);
        }

        /// <summary>
        /// Gets Events by its ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Event by id</returns>
        // GET api/events/{id}
        [HttpGet("{id}", Name = "GetEventById")]
        public ActionResult<Event> GetEventById(int id)
        {
            var eventfound = new Event();
            if (!_cache.TryGetValue("SingleEvent" + id, out eventfound))
            {
                Console.WriteLine("No cache found, creating new");

                eventfound = _repository.GetEventById(id);
                if (eventfound == null)
                {
                    return NotFound();
                }
                eventfound = _repository.GetRequestEvent(eventfound);

                var callPath = Request;
                _repository.AddLinks(eventfound, callPath);

                _cache.Set("SingleEvent" + id, eventfound, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(10)));
            }
            else
            {
                Console.WriteLine("Cache found, pulling out");
            }
            return Ok(eventfound);
        }



        /// <summary>
        /// Creates new Event.
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST api/event
        ///     {
        ///         "eventName": "Vakaro sokiai",
        ///         "eventDescription": "aprasymas apie sokius su kazkuo ir kazkur",
        ///         "eventDate": "2020-06-20",
        ///         "streetLocation": "Jurbarkiniu g. 12", Vilnius
        ///     }
        ///     
        /// </remarks>
        /// <param name="newEvent"></param> 
        /// <returns>Created Event</returns>
        // POST api/events/
        [HttpPost(Name = "CreateEvent")] // GALIMA neduot eventId (DB pati susigeneruos), bet bus 500 error nes nesusigaudys createdAtRoute.
        public ActionResult<Event> CreateCommand(Event newEvent)
        {
            _repository.CreateEvent(newEvent);
            _repository.SaveChanges();

            return CreatedAtRoute(nameof(GetEventById), new { id = newEvent.eventId }, newEvent);
        }

        /// <summary>
        /// Updates new Event.
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     PUT api/event
        ///     {
        ///         "eventName": "Vakaro sokiai",
        ///         "eventDescription": "aprasymas apie sokius su kazkuo ir kazkur",
        ///         "eventDate": "2020-06-20",
        ///         "streetLocation": "Jurbarkiniu g. 12", Vilnius
        ///     }
        ///     
        /// </remarks>
        /// <param EventId="id"></param> 
        /// <param name="updatedEvent"></param> 
        /// <returns></returns>
        // PUT api/events/{id}
        [HttpPut("{id}", Name = "UpdateEvent")] // Nereikia eventId Json, susiranda pagal http put id.
        public ActionResult UpdateEvent(int id, Event updatedEvent)
        {
            var existingEvent = _repository.GetEventById(id);
            if (existingEvent == null)
            {
                return NotFound();
            }

            _repository.UpdateEvent(id, updatedEvent);

            return NoContent();
        }

        /// <summary>
        /// Deletes Event by its ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE api/events/{id}
        [HttpDelete("{id}", Name = "DeleteEvent")]
        public ActionResult DeleteEvent(int id)
        {
            var findEvent = _repository.GetEventById(id);
            if (findEvent == null)
            {
                return NotFound();
            }
            _repository.DeleteEvent(findEvent);
            _repository.SaveChanges();

            return NoContent();
        }

        // ATTEND EVENTS

        /// <summary>
        /// Gets Events with participants by its ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Event with participants by id</returns>
        // GET api/events/attend/{id}
        [HttpGet("attend/{id}", Name = "GetEventAttendants")]
        public ActionResult<Event> GetEventAttendants(int id)
        {
            var eventDone = new Event();
            var eventfound = new Event();
            if (!_cache.TryGetValue("SingleAttendEvent" + id, out eventfound))
            {
                Console.WriteLine("No cache found, creating new");

                eventfound = _repository.GetEventAttendants(id);
                if (eventfound == null)
                {
                    return NotFound();
                }
                eventDone = _repository.GetRequestEvent(eventfound);
                _cache.Set("SingleAttendEvent" + id, eventfound, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(10)));
            }
            else
            {
                Console.WriteLine("Cache found, pulling out");
            }
            return Ok(eventDone);
        }

        /// <summary>
        /// Adds new parcipant to event by thier IDs.
        /// </summary>
        /// <param eventId="Event ID"></param> 
        /// <param personId="Person ID"></param> 
        /// <returns>Added person to Event</returns>
        // POST api/events/attend/{id}/{id}
        [HttpPost("attend/{eventId}/{personId}")]
        public ActionResult AddParticipant(int eventId, int personId)
        {
            var existingEvent = _repository.GetEventById(eventId);
            if (existingEvent == null)
            {
                return NotFound(" Event Not Found ");
            }

            _repository.AddPersonToEvent(eventId, personId);

            return CreatedAtRoute(nameof(GetEventAttendants), new { id = eventId }, eventId);
            //return NoContent();
        }

        /// <summary>
        /// Deletes Event participant by its ID
        /// </summary>
        /// <param eventId="Event ID"></param> 
        /// <param personId="Person ID"></param> 
        /// <returns></returns>
        // DELETE api/events/attend/{id}/{id}
        [HttpDelete("attend/{eventId}/{personId}")]
        public ActionResult DeletePaticipant(int eventId, int personId)
        {
            var existingEvent = _repository.GetEventById(eventId);
            if (existingEvent == null)
            {
                return NotFound(" Event Not Found ");
            }

            _repository.RemovePersonFromEvent(eventId, personId);

            return NoContent();
        }

        // PEOPLE

        /// <summary>
        /// Gets the list of all People
        /// </summary>
        /// <returns>The list of People</returns>
        // GET: api/events
        [HttpGet("person/")]
        public ActionResult<IEnumerable<Event>> GetAllPeeps()
        {
            var allPeeps = new List<Person>();
            if (!_cache.TryGetValue("AllPeepsList", out allPeeps))
            {
                Console.WriteLine("No cache found, creating new");

                allPeeps = _repository.GetAllPeeps();
                var callPath = Request;
                foreach (var a in allPeeps)
                {
                   _repository.AddLinksPerson(a, callPath);
                }

                _cache.Set("AllPeepsList", allPeeps, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(10)));
            }
            else
            {
                Console.WriteLine("Cache found, pulling out");
            }
            return Ok(allPeeps);
        }

        /// <summary>
        /// Gets Person by its ID
        /// </summary>
        /// <param id="PersonId"></param>
        /// <returns>Person by id</returns>
        // GET api/events/people/{id}
        [HttpGet("person/{id}", Name = "GetPersonById")]
        public ActionResult<Event> GetPersonById(int id)
        {
            var personFound = new Person();
            if (!_cache.TryGetValue("SinglePerson" + id, out personFound))
            {
                Console.WriteLine("No cache found, creating new");

                personFound = _repository.GetPersonById(id);
                var callPath = Request;
                _repository.AddLinksPer(personFound, callPath);
                if (personFound == null)
                {
                    return NotFound();
                }
                _cache.Set("SinglePerson" + id, personFound, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(10)));
            }
            else
            {
                Console.WriteLine("Cache found, pulling out");
            }
            return Ok(personFound);
        }

        /// <summary>
        /// Creates new Person.
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST api/event/people
        ///     {
        ///         "name": "Tomas",
        ///         "lastName": "Kaviaras",
        ///         "phoneNumber": 8625685411,
        ///     }
        ///     
        /// </remarks>
        /// <param name="newPerson"></param> 
        /// <returns>Created Person</returns>
        // POST api/events/people/
        [HttpPost("person/")] 
        public ActionResult<Event> CreatePerson(Person newPerson)
        {
            _repository.CreatePerson(newPerson);
            _repository.SaveChanges();

            return CreatedAtRoute(nameof(GetPersonById), new { id = newPerson.personId }, newPerson);
        }

        /// <summary>
        /// Updates new Person.
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     PUT api/event/people/{Id}
        ///     {
        ///         "name": "Tomas",
        ///         "lastName": "Kaviaras",
        ///         "phoneNumber": 8625685411,
        ///     }
        ///     
        /// </remarks>
        /// <param id="PersonId"></param> 
        /// <param name="updatedPerson"></param> 
        /// <returns></returns>
        // PUT api/events/people/{id}
        [HttpPut("person/{id}")] // Nereikia eventId Json, susiranda pagal http put id.
        public ActionResult UpdatePerson(int id, Person updatedPerson)
        {
            var exsistingPerson = _repository.GetPersonById(id);
            if (exsistingPerson == null)
            {
                return NotFound();
            }

            _repository.UpdatePerson(id, updatedPerson);

            return NoContent();
        }

        /// <summary>
        /// Deletes Person by its ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE api/events/people/{id}
        [HttpDelete("person/{id}")]
        public ActionResult DeletePerson(int id)
        {
            var findPerson = _repository.GetPersonById(id);
            if (findPerson == null)
            {
                return NotFound();
            }
            _repository.DeletePerson(findPerson);
            _repository.SaveChanges();

            return NoContent();
        }

    }
}
