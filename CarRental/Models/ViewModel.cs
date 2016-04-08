using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DHTMLX.Scheduler;

namespace CarRental.Models
{
    /// <summary>
    /// View model
    /// </summary>
    public class ViewModel
    {
        public DHXScheduler Scheduler { get; set; }
        public int CategoryCount { get; set; }
        public int ChildCount { get; set; }
    }

    /// <summary>
    /// Form state
    /// </summary>
    public class FormState
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? TimeFrom { get; set; }
        public int? TimeTo { get; set; }
        public int? Type { get; set; }
        public string PriceRange { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool DateFilter { get; set; }
    }

    public class CarParentModel
    {
        public int key;
        public string label;
        public int id;
        public string link;
        public decimal price;
        public bool parent;
        public bool open;
        public List<CarChildModel> children;
    }

    public class CarChildModel
    {
        public int key { get; set; }
        public string label { get; set; }
        public string name { get; set; }
        public int carId { get; set; }
        public int carNumber { get; set; }
    }
}