using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestConsoleApp
{
    public static class Extensions
    {
        public static void Dump(this List<ScheduleSlot> slotsList)
        {
            Console.WriteLine(Environment.NewLine);
            slotsList.ForEach(x => Console.WriteLine("StartTime : " + x.StartTime + ", EndTime : " + x.EndTime + ", TimeSpan : " + (x.EndTime - x.StartTime)));
        }
    }

    public class ScheduleSlot
    {
        public TimeSpan EndTime { get; set; }
        public TimeSpan StartTime { get; set; }
    }

    internal class Program
    {
        public static string guestBounds = "['10:00', '18:30']";
        public static string guestSchedule = "[['10:00', '11:30'],['12:30', '14:30'],['14:30', '15:00'],['16:00', '17:00']]";
        public static string hostBounds = "['9:00', '20:00']";
        public static string hostSchedule = "[['9:00', '10:30'],['12:00', '13:00'],['16:00', '18:00']]";

        private static bool Filter(ScheduleSlot slot, ScheduleSlot hostSpareTimeSlot) =>
            (slot.StartTime >= hostSpareTimeSlot.StartTime && slot.StartTime <= hostSpareTimeSlot.EndTime)
                            || (slot.EndTime >= hostSpareTimeSlot.StartTime && slot.EndTime <= hostSpareTimeSlot.EndTime);

        private static IList<ScheduleSlot> GenerateAvailableTimeSlots(IList<ScheduleSlot> scheduleList, ScheduleSlot bounds)
        {
            List<ScheduleSlot> spareTimeSlots = new List<ScheduleSlot>();
            for (int i = 0; i < scheduleList.Count; i++)
            {
                if (i == 0 && scheduleList[i].StartTime > bounds.StartTime)
                {
                    spareTimeSlots.Add(new ScheduleSlot
                    {
                        StartTime = bounds.StartTime,
                        EndTime = scheduleList[i].StartTime
                    });
                }
                if (i == scheduleList.Count - 1)
                {
                    if (scheduleList[i].EndTime < bounds.EndTime)
                    {
                        spareTimeSlots.Add(new ScheduleSlot
                        {
                            StartTime = scheduleList[i].EndTime,
                            EndTime = bounds.EndTime
                        });
                    }
                }
                else if (scheduleList[i].EndTime != scheduleList[i + 1].StartTime)
                {
                    spareTimeSlots.Add(new ScheduleSlot
                    {
                        StartTime = scheduleList[i].EndTime,
                        EndTime = scheduleList[i + 1].StartTime
                    });
                }
            }
            return spareTimeSlots;
        }

        private static ScheduleSlot GenerateBoundSlot(string schedule)
        {
            var timeSlot = JArray.Parse(schedule.ToString());
            return new ScheduleSlot
            {
                StartTime = Convert.ToDateTime(timeSlot[0].ToString()).TimeOfDay,
                EndTime = Convert.ToDateTime(timeSlot[1].ToString()).TimeOfDay
            };
        }

        private static List<ScheduleSlot> GenerateScheduleList(string schedule)
        {
            List<ScheduleSlot> scheduleList = new List<ScheduleSlot>();
            foreach (var slot in JArray.Parse(schedule))
            {
                var timeSlot = JArray.Parse(slot.ToString());
                scheduleList.Add(new ScheduleSlot
                {
                    StartTime = Convert.ToDateTime(timeSlot[0].ToString()).TimeOfDay,
                    EndTime = Convert.ToDateTime(timeSlot[1].ToString()).TimeOfDay
                });
            }
            return scheduleList;
        }

        private static void Main(string[] args)
        {
            var preferredMeetingDuration = TimeSpan.Parse("00:30");
            var hostScheduleList = GenerateScheduleList(hostSchedule);
            var hostBound = GenerateBoundSlot(hostBounds);
            var guestScheduleList = GenerateScheduleList(guestSchedule);
            var guestBound = GenerateBoundSlot(guestBounds);

            var hostSpareTimeSlots = GenerateAvailableTimeSlots(hostScheduleList, hostBound);

            var guestSpareTimeSlots = GenerateAvailableTimeSlots(guestScheduleList, guestBound);

            List<ScheduleSlot> availableTimeSlotList = new List<ScheduleSlot>();
            for (int i = 0; i < hostSpareTimeSlots.Count; i++)
            {
                foreach (var guestSlot in guestSpareTimeSlots.Where(x => Filter(x, hostSpareTimeSlots[i])))
                {
                    availableTimeSlotList.Add(
                        new ScheduleSlot
                        {
                            StartTime = (hostSpareTimeSlots[i].StartTime >= guestSlot.StartTime) ? hostSpareTimeSlots[i].StartTime : guestSlot.StartTime,
                            EndTime = (guestSlot.EndTime >= hostSpareTimeSlots[i].EndTime) ? hostSpareTimeSlots[i].EndTime : guestSlot.EndTime
                        }
                    );
                }
            }

            Console.WriteLine(Environment.NewLine);
            Console.WriteLine("---------- Available Time Slots -----------");
            availableTimeSlotList
                .Where(x => (x.EndTime - x.StartTime) >= preferredMeetingDuration).ToList()
                .Dump();

            Console.ReadKey();
        }
    }
}
