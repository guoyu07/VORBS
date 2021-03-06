﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using VORBS.Models;
using VORBS.DAL;
using VORBS.Models.DTOs;
using VORBS.Services;
using VORBS.DAL.Repositories;
using VORBS.Utils;

namespace VORBS.API
{
    [RoutePrefix("api/availability")]
    public class AvailabilityController : ApiController
    {
        private NLog.Logger _logger;
        private VORBSContext db;
        private AvailabilityService _availabilityService;

        private IBookingRepository _bookingRepository;
        private ILocationRepository _locationRepository;
        private IRoomRepository _roomsRepository;

        public AvailabilityController(VORBSContext context, 
            IBookingRepository bookingRepository,
            ILocationRepository locationRepository,
            IRoomRepository roomsRepository)
        {
            _logger = NLog.LogManager.GetCurrentClassLogger();
            db = context;
            _availabilityService = new AvailabilityService(bookingRepository, roomsRepository, locationRepository);

            _bookingRepository = bookingRepository;
            _locationRepository = locationRepository;
            _roomsRepository = roomsRepository;

            _logger.Trace(LoggerHelper.InitializeClassMessage());
        }

        [HttpGet]
        [Route("{location}/{start:DateTime}/{smartRoom:bool}")]
        public List<RoomDTO> GetAllRoomBookingsForLocation(string location, DateTime start, bool smartRoom)
        {
            List<RoomDTO> rooms = new List<RoomDTO>();

            if (location == null)
            {
                List<RoomDTO> result = new List<RoomDTO>();
                _logger.Debug("Location is null");
                _logger.Trace(LoggerHelper.ExecutedFunctionMessage(result, location, start, smartRoom));
                return result;
            }
                

            List<Room> roomData = new List<Room>();

            try
            {
                var availableRooms = db.Rooms.Where(x => x.Location.Name == location && x.Active == true)
                    .OrderBy(r => r.SeatCount)
                    .ToList();

                if (smartRoom)
                    availableRooms = availableRooms.Where(r => r.SmartRoom == true).ToList();

                roomData.AddRange(availableRooms);

                roomData.ForEach(x => rooms.Add(new RoomDTO()
                {
                    ID = x.ID,
                    RoomName = x.RoomName,
                    PhoneCount = x.PhoneCount,
                    ComputerCount = x.ComputerCount,
                    SeatCount = x.SeatCount,
                    SmartRoom = x.SmartRoom,
                    Bookings = x.Bookings.Where(b => b.StartDate.Date == start.Date).Select(b =>
                    {
                        BookingDTO bDto = new BookingDTO()
                        {
                            ID = b.ID,
                            Owner = b.Owner,
                            StartDate = b.StartDate,
                            EndDate = b.EndDate,
                            IsSmartMeeting = b.IsSmartMeeting
                        };
                        return bDto;
                    }).ToList()
                }));
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Unable to get available rooms for location: " + location, ex);
            }
            _logger.Trace(LoggerHelper.ExecutedFunctionMessage(rooms, location, start, smartRoom));
            return rooms;
        }

        [HttpGet]
        [Route("{location}/{start:DateTime}/{smartRoom:bool}/{end:DateTime}/{numberOfPeople:int}")]
        public List<RoomDTO> GetAvailableRoomsForLocation(string location, DateTime start, bool smartRoom, DateTime end, int numberOfPeople)
        {
            List<RoomDTO> rooms = new List<RoomDTO>();

            if (location == null)
            {
                List<RoomDTO> result = new List<RoomDTO>();
                _logger.Debug("Location is null");
                _logger.Trace(LoggerHelper.ExecutedFunctionMessage(result, location, start, smartRoom, end, numberOfPeople));
                return result;
            }
                
            List<Room> roomData = new List<Room>();

            try
            {
                var availableRooms = db.Rooms.Where(x => x.Location.Name == location && x.SeatCount >= numberOfPeople && x.Active == true &&
                                               (x.Bookings.Where(b => start < b.EndDate && end > b.StartDate).Count() == 0)) //Do any bookings overlap
                                .OrderBy(r => r.SeatCount)
                                .ToList();

                if (smartRoom)
                    availableRooms = availableRooms.Where(r => r.SmartRoom == true).ToList();

                roomData.AddRange(availableRooms);

                roomData.ForEach(x => rooms.Add(new RoomDTO()
                {
                    ID = x.ID,
                    RoomName = x.RoomName,
                    PhoneCount = x.PhoneCount,
                    ComputerCount = x.ComputerCount,
                    SeatCount = x.SeatCount,
                    SmartRoom = x.SmartRoom,
                    Bookings = x.Bookings.Where(b => b.StartDate.Date == start.Date && b.EndDate.Date == end.Date).Select(b =>
                    {
                        BookingDTO bDto = new BookingDTO()
                        {
                            ID = b.ID,
                            Owner = b.Owner,
                            StartDate = b.StartDate,
                            EndDate = b.EndDate,
                            IsSmartMeeting = b.IsSmartMeeting
                        };
                        return bDto;
                    }).ToList()
                }));
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Unable to get available rooms for location: " + location, ex);
            }
            _logger.Trace(LoggerHelper.ExecutedFunctionMessage(rooms, location, start, smartRoom, end, numberOfPeople));
            return rooms;
        }

        [HttpGet]
        [Route("{location}/{start:DateTime}/{smartRoom:bool}/{end:DateTime}/{numberOfPeople:int}/{existingBookingId:int}")]
        public List<RoomDTO> GetAvailableRoomsForLocation(string location, DateTime start, bool smartRoom, DateTime end, int numberOfPeople, int existingBookingId)
        {
            List<RoomDTO> rooms = new List<RoomDTO>();

            if (location == null)
            {
                List<RoomDTO> result = new List<RoomDTO>();
                _logger.Debug("Location is null");
                _logger.Trace(LoggerHelper.ExecutedFunctionMessage(result, location, start, smartRoom, end, numberOfPeople, existingBookingId));
                return result;
            }

            List<Room> roomData = new List<Room>();

            try
            {

                List<Room> availableRooms = new List<Room>();

                Booking existingBooking = _bookingRepository.GetById(existingBookingId);

                List<Booking> clashedBookings;
                existingBooking.StartDate = start;
                existingBooking.EndDate = end;

                bool meetingClash = _availabilityService.DoesMeetingClash(existingBooking, out clashedBookings);

                if (meetingClash && clashedBookings.Count == 1 && clashedBookings[0].ID == existingBooking.ID)
                    availableRooms.Add(existingBooking.Room);

                else
                {
                    availableRooms = db.Rooms.Where(x => x.Location.Name == location && x.SeatCount >= numberOfPeople && x.Active == true && x.SmartRoom == smartRoom &&
                                               (x.Bookings.Where(b => start < b.EndDate && end > b.StartDate && b.ID != existingBookingId).Count() == 0)) //Do any bookings overlap
                                .OrderBy(r => r.SeatCount)
                                .ToList();
                }
                roomData.AddRange(availableRooms);

                roomData.ForEach(x => rooms.Add(new RoomDTO()
                {
                    ID = x.ID,
                    RoomName = x.RoomName,
                    PhoneCount = x.PhoneCount,
                    ComputerCount = x.ComputerCount,
                    SeatCount = x.SeatCount,
                    SmartRoom = x.SmartRoom,
                    Bookings = x.Bookings.Where(b => b.StartDate.Date == start.Date && b.EndDate.Date == end.Date).Select(b =>
                    {
                        BookingDTO bDto = new BookingDTO()
                        {
                            ID = b.ID,
                            Owner = b.Owner,
                            StartDate = b.StartDate,
                            EndDate = b.EndDate,
                            IsSmartMeeting = b.IsSmartMeeting
                        };
                        return bDto;
                    }).ToList()
                }));
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Unable to get available rooms for location: " + location, ex);
            }
            _logger.Trace(LoggerHelper.ExecutedFunctionMessage(rooms, location, start, smartRoom, end, numberOfPeople, existingBookingId));
            return rooms;
        }
    }
}
