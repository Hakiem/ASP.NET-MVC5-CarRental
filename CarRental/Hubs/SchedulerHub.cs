using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR;

namespace CarRental
{
    [HubName("schedulerHub")]
    public class SchedulerHub : Hub
    {
        public void Send(string update)
        {
            this.Clients.All.addMessage(update);
        }
    }
}