using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Portal.Web.Models.JobScheduler
{
    public class JobScheduler
    {
        public readonly static JobScheduler inst = new JobScheduler();
        private static IScheduler scheduler;
        private readonly Entities db;
        private JobScheduler() { 
            scheduler =  StdSchedulerFactory.GetDefaultScheduler();
            db = new Entities();
        }

        public void Start()
        {
            scheduler.Start();
            IJobDetail job = JobBuilder.Create<EmailJob>().Build();
            List<Scheduler> tasks = GetAllTasks();
            tasks.ForEach(task => AddTask(task));
        }
        public void AddTask(Scheduler task)
        {
            TriggerBuilder builder;
            const int Week = 7;
                   
            if (task.StartDate != null) {
                builder = TriggerBuilder.Create()
                   .WithIdentity(task.SchedulerId.ToString())
                   .StartAt((DateTime)task.StartDate);

                if (task.Interval == 1) {
                    builder.WithCalendarIntervalSchedule(time => time.WithIntervalInDays(Week)); 
                }
                else {
                    builder.WithCalendarIntervalSchedule(time => time.WithIntervalInMonths(1)); 
                }

                IJobDetail job = JobBuilder.Create<EmailJob>().Build();
                ITrigger trigger = builder.Build();

                scheduler.ScheduleJob(job, trigger);
            }
        }

        public void CreateTask(Scheduler task)
        {
            db.Schedulers.Add(task);
            db.SaveChanges();
            AddTask(task);
        }

        public void RemoveTask(Scheduler task)
        {
            db.Schedulers.Remove(task);
            db.SaveChanges();
            UnscheduleTask(task.SchedulerId.ToString());
        }

        public void UnscheduleTask(string triggerName)
        {
            TriggerKey key = new TriggerKey(triggerName);
            scheduler.UnscheduleJob(key);
        }

        public void Task(Scheduler schedul)
        {
            var param = new Dictionary<string, string>();
            UserProfile profile = GetUserByID(schedul.ReciverId);
            string email = profile.Email;
            string username = profile.Firstname;
            param["USERNAME"] = username;
            try 
            { 
                string pdfName = PdfMergeStreamer.MakePDF(schedul);
                EmailModel.inst.Send(email, "Statistic", param, pdfName);
            }
            catch (Exception e)
            {
                LogModel.inst.Write(e.Message);
            }
        }

        private List<Scheduler> GetAllTasks()
        {
            return (from sched in db.Schedulers select sched).ToList();
        }

        public Scheduler GetSchedulerByID(int id)
        {
            return (from sched in db.Schedulers where sched.SchedulerId == id select sched).FirstOrDefault();
        
        }
        public List<Scheduler> GetUserTasks(int userId)
        {
            return (from sched in db.Schedulers where sched.ReciverId == userId select sched).ToList();
        }
        public UserProfile GetUserByID(int id)
        {
            return (from users in db.UserProfiles where users.UserProfileId == id select users).FirstOrDefault();
        }

       
    }
}