using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Collections;
using System.Collections.Generic;
using DHTMLX.Scheduler;
using DHTMLX.Scheduler.Controls;
using DHTMLX.Scheduler.Data;
using DHTMLX.Common;
using CarRental.Models;
using System;
using System.Security;
using Newtonsoft.Json;
using System.Web.Script.Serialization;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace CarRental.Controllers
{
    public class HomeController : Controller
    {
        /// <summary>
        /// Index
        /// </summary>
        /// <returns>View with model</returns>
        public ActionResult Index(FormState state)
        {
            if(!isDefaultFilter(state)){
                ViewData["filtered"] = true;
            }

            state = _PrepareFormState(state);
            var scheduler = new DHXScheduler();
            scheduler.Extensions.Add(SchedulerExtensions.Extension.Collision);
            scheduler.Extensions.Add(SchedulerExtensions.Extension.Minical);
           
            scheduler.Extensions.Add(SchedulerExtensions.Extension.Tooltip);
            
           
          
            scheduler.Extensions.Add(SchedulerExtensions.Extension.ActiveLinks);
            scheduler.Extensions.Add(SchedulerExtensions.Extension.LiveUpdates);
            scheduler.Config.fix_tab_position = false;
            scheduler.EnableDynamicLoading(SchedulerDataLoader.DynamicalLoadingMode.Week);
            //call custom template initialization
            scheduler.BeforeInit.Add("defineTemplates();");
            scheduler.AfterInit.Add("afterInit()");
            scheduler.Config.time_step = 60;
            scheduler.InitialValues.Add("text", "");
             
            //set max dispay events in month
            scheduler.Config.max_month_events = 4;

            //if 'Pick Up Date' selected - make it initial date for calendar
            if (state.DateFrom != null)
            {
                scheduler.InitialDate = state.DateFrom.Value;
            }

            var context = new RentalDataContext();
            
            //selecting cars according to form values
            var cars = _SelectCars(context, state).ToList();
            
            //if no cars found - show message and load default set
            if (cars.Count() == 0)
            {
                ViewData["Message"] = "Nothing was found on your request";
                cars = _SelectCars(context).ToList();//select default set of events
            }



            var list = new List<CarChildModel>();

            _ConfigureLightbox(scheduler, cars);

            //load cars to the timeline view
            _ConfigureViews(scheduler, cars);

            //assign ViewData values
            _UpdateViewData(scheduler, context, state);

            //data loading/saving settings
          
            scheduler.LoadData = true;
            scheduler.EnableDataprocessor = true;
            scheduler.Skin = DHXScheduler.Skins.Terrace;

            scheduler.SaveAction = Url.Action("Save", "Home");
            scheduler.DataAction = Url.Action("Data", "Home");

            //collect model
            var model = new ViewModel();

            model.Scheduler = scheduler;
            model.CategoryCount = cars.Count();
            model.ChildCount = list.Count();
            return View(model);
        }
        
        /// <summary>
        /// Update view data
        /// </summary>
        /// <param name="scheduler">scheduler</param>
        /// <param name="context">context</param>
        /// <param name="state">state</param>
        protected void _UpdateViewData(DHXScheduler scheduler, RentalDataContext context, FormState state)
        {
            ViewData["PriceRange"] = _CreatePriceSelect(scheduler, state.PriceRange);
            ViewData["Type"] = _CreateTypeSelect(context.Types, state, context);
            ViewData["DateFrom"] = state.DateFrom;
            ViewData["DateTo"] = state.DateTo;
            ViewData["TimeFrom"] = _CreateTimeSelect(scheduler, state.TimeFrom);
            ViewData["TimeTo"] = _CreateTimeSelect(scheduler, state.TimeTo);
            ViewData["DateFilter"] = state.DateFilter;
        }

        protected bool isDefaultFilter(FormState state)
        {
            return (state.DateFrom != null &&
                state.DateTo != null &&
                string.IsNullOrEmpty(state.PriceRange) &&
                (state.TimeFrom != null || state.TimeFrom.Value == 0) &&
                (state.TimeTo != null || state.TimeTo.Value == 0) &&
                state.Type != null);
        }

        public ContentResult Data(DateTime from, DateTime to)
        {
            var state = _PrepareFormState(new FormState
            {
                DateFrom = from,
                DateTo = to
            });

            var orders = _GetOrders(new RentalDataContext(), state);
            return Content(new SchedulerAjaxData(orders), "text/json");
        }

        public ActionResult FilterCars(FormState state)
        {
            var context = new RentalDataContext();
            var cars = _SelectCars(context, _PrepareFormState(state));
            return Content(new SchedulerAjaxData(cars), "text/json");
        }


        /// <summary>
        /// Create type select
        /// </summary>
        /// <param name="types">types</param>
        /// <param name="selected">selected</param>
        /// <returns>list with select list item</returns>
        private List<SelectListItem> _CreateTypeSelect(IEnumerable<Type> types, FormState state, RentalDataContext context)
        {
            var selected = state.Type;
            var typesList = new List<SelectListItem>()
            {
                new SelectListItem(){Value = "", Text = "Any"}
            };

            foreach (var type in types)
            {
                state.Type = type.Id;
                var item = new SelectListItem() { Value = type.Id.ToString(), Text = string.Format("{0}: {1} cars", type.title, _SelectCars(context, state).Count())};
                if (selected != null && type.Id == selected.Value)
                    item.Selected = true;
                typesList.Add(item);

            }
            return typesList;
        }

        /// <summary>
        /// Create price select
        /// </summary>
        /// <param name="scheduler">scheduler</param>
        /// <param name="selected">selected</param>
        /// <returns>list with select list item</returns>
        private List<SelectListItem> _CreatePriceSelect(DHXScheduler scheduler, string selected)
        {
            var priceRanges = new string[] { "50-80", "80-120", "120-150" };
            var prices = new List<SelectListItem>(){
                new SelectListItem(){Value = "", Text = "Any"}
            };

            foreach (var pr in priceRanges)
            {
                var item = new SelectListItem() { Value = pr, Text = string.Format("${0}", pr) };
                if (pr == selected)
                    item.Selected = true;
                prices.Add(item);
            }
            return prices;
        }

        /// <summary>
        /// Select cars
        /// </summary>
        /// <param name="context">context</param>
        /// <returns>IQueryable of car</returns>
        protected IQueryable<Car> _SelectCars(RentalDataContext context)
        {
            return _SelectCars(context, null);
        }

        /// <summary>
        /// Select cars
        /// </summary>
        /// <param name="context">context</param>
        /// <param name="state">state</param>
        /// <returns>IQueryable of car</returns>
        protected IQueryable<Car> _SelectCars(RentalDataContext context, FormState state)
        {
            IQueryable<Car> cars = from car in context.Cars select car;
            if (state == null)
                return cars;

            //filter by car type
            if (state.Type != null)
            {
                cars = cars.Where(c => c.TypeId == state.Type.Value);
            }

            //filter by date
            if (state.DateFilter && state.DateFrom != null && state.DateTo != null)
            {
                cars = from car in cars
                       where car.Orders.Where(o => o.end_date > state.DateFrom.Value && o.start_date < state.DateTo.Value).Count() == 0
                       select car;
            }
          
            //filter by price
            if (state.MinPrice != null && state.MaxPrice != null)
            {
                cars = cars.Where(c => c.Price <= state.MaxPrice.Value && c.Price >= state.MinPrice.Value);
            }

            return cars;
        }

        /// <summary>
        /// Configure views
        /// </summary>
        /// <param name="scheduler">scheduler</param>
        /// <param name="cars">cars</param>
        protected void _ConfigureViews(DHXScheduler scheduler, IEnumerable cars)
        {

            scheduler.Views.Clear();

            //create TimeLines views 
            scheduler.Views.Add(CreateDayTimeline(cars));
            scheduler.Views.Add(CreateWeekTimeline(cars));
            scheduler.Views.Add(CreateTwoWeeksTimeline(cars));

            // create MonthView
            var month = new MonthView();
            month.TabStyle = "left: 181px;";
            month.TabClass = "dhx_cal_tab_last";
            scheduler.Views.Add(month);

            scheduler.InitialView = scheduler.Views[0].Name;
        }

        /// <summary>
        /// Create view for time line
        /// </summary>
        /// <param name="cars">cars</param>
        /// <returns>TimelineView object</returns>
        protected TimelineView CreateDayTimeline(IEnumerable cars)
        {
            var unitsDay = GenericTimeline("Orders", cars);
            unitsDay.X_Step = 2;
            unitsDay.X_Length = 12;
            unitsDay.X_Size = 12;

           
            unitsDay.TabStyle = "left: 38px";
            unitsDay.TabClass = "dhx_cal_tab_first";
            return unitsDay;
        }
        protected TimelineView GenericTimeline(string name, IEnumerable cars)
        {
            var timeline = new TimelineView(name, "section_id");

            //width of the first column
            timeline.Dx = 149;
            timeline.AddOptions(cars);
            //row height
            timeline.Dy = 38;

            //rent boxes height
            timeline.EventDy = timeline.Dy - 5;
            timeline.FolderDy = 114;
            timeline.FolderEventsAvailable = false;
            timeline.ResizeEvents = false;
            timeline.SectionAutoheight = false;
            timeline.RenderMode = TimelineView.RenderModes.Tree;
            return timeline;
        }
        /// <summary>
        /// Create view for time line
        /// </summary>
        /// <param name="cars">cars</param>
        /// <returns>TimelineView object</returns>
        protected TimelineView CreateWeekTimeline(IEnumerable cars)
        {
            var unitsWeek = GenericTimeline("Orders_Week", cars);
            unitsWeek.X_Step = 1;
            unitsWeek.X_Size = 7;
            unitsWeek.X_Unit = TimelineView.XScaleUnits.Day;
            unitsWeek.Label = "Week <p class=\"week_tab_text\"> 1 </p>";
            unitsWeek.TabStyle = "font-size:10px;line-height:1.2; width:40px;left:99px;";

            return unitsWeek;
        }

        /// <summary>
        /// Create view for time line
        /// </summary>
        /// <param name="cars">cars</param>
        /// <returns>TimelineView object</returns>
        protected TimelineView CreateTwoWeeksTimeline(IEnumerable cars)
        {
            var unitsTwoWeek = GenericTimeline("Orders_TwoWeeks", cars);
            unitsTwoWeek.X_Step = 1;
            unitsTwoWeek.X_Size = 14;

            unitsTwoWeek.X_Unit = TimelineView.XScaleUnits.Day;
            unitsTwoWeek.Label = "Week <p class=\"week_tab_text\"> 2 </p>";
            unitsTwoWeek.TabStyle = "font-size:10px;line-height:1.2; width:40px;left:140px;";

            return unitsTwoWeek;
        }

        /// <summary>
        /// Configure lightbox
        /// </summary>
        /// <param name="scheduler">scheduler</param>
        /// <param name="cars">cars</param>
        protected void _ConfigureLightbox(DHXScheduler scheduler, IEnumerable cars)
        {
            scheduler.Lightbox.Add(new LightboxText("text", "Contact details") { Height = 42, Focus = true });
            scheduler.Lightbox.Add(new LightboxText("description", "Note") { Height = 63 });
            var select = new LightboxSelect("section_id", "Car"); 
            scheduler.Lightbox.Add(select);
           // scheduler.Lightbox.Add(new LightboxText("pick_location", "Pick up location") { Height = 21 });
           // scheduler.Lightbox.Add(new LightboxText("drop_location", "Drop off location") { Height = 21 });
            select.ServerList = "SelectOptions";
            
            scheduler.Lightbox.Add(new LightboxMiniCalendar("time", "Time period"));
        }


        protected FormState _PrepareFormState(FormState state)
        {
            if (state != null)
            {
                DateTime? minTime = null;
                DateTime? maxTime = null;
                //try to parse time range
                if (state.DateFrom != null)
                {
                    minTime = state.DateFrom;
                    if (state.TimeFrom != null) minTime.Value.AddHours(state.TimeFrom.Value);
                    state.DateFrom = minTime;
                }
                if (state.DateTo != null)
                {
                    maxTime = state.DateTo;
                    if (state.TimeTo != null) state.DateTo.Value.AddHours(state.TimeTo.Value);
                    state.DateTo = maxTime;
                }

                //filter by price
                if (!string.IsNullOrEmpty(state.PriceRange))
                {
                    var parts = state.PriceRange.Split('-');
                    var min = decimal.Parse(parts[0]);
                    var max = decimal.Parse(parts[1]);

                    state.MinPrice = min;
                    state.MaxPrice = max;
                }
                return state;
            }
            else
            {
                return new FormState();
            }
        }
        protected IEnumerable<Order> _GetOrders(RentalDataContext context, FormState state)
        {
            // all orders
            var orders = from order in context.Orders select order ;

            //filter by car type
            if (state.DateFilter && state.DateFrom != null && state.DateTo != null)
            {
                //select cars, which are available in specified time range
                orders = orders.Where(o => o.end_date > state.DateFrom.Value && o.start_date < state.DateTo.Value);

            }

            return orders.ToList();
        }


        /// <summary>
        /// Save
        /// </summary>
        /// <param name="id">id</param>
        /// <param name="actionValues">action values</param>
        /// <returns>Content result</returns>
        public ContentResult Save(int? id, FormCollection actionValues)
        {

            var action = new DataAction(actionValues);

            RentalDataContext data = new RentalDataContext();

            try
            {
                var changedEvent = (Order)DHXEventsHelper.Bind(typeof(Order), actionValues);

                // find car and change order

                switch (action.Type)
                {
                    case DataActionTypes.Insert:
         
                        data.Orders.InsertOnSubmit(changedEvent);
                        break;
                    case DataActionTypes.Delete:                     
                        changedEvent = data.Orders.SingleOrDefault(ev => ev.id == action.SourceId);
                        data.Orders.DeleteOnSubmit(changedEvent);
                        break;
                    default:                       
                        var eventToUpdate = data.Orders.SingleOrDefault(ev => ev.id == action.SourceId);
                        EventUpdate(eventToUpdate, changedEvent);
                        break;
                }
                data.SubmitChanges();
                action.TargetId = changedEvent.id;
            }
            catch
            {
                action.Type = DataActionTypes.Error;
            }

            return Content(new AjaxSaveResponse(action),"text/xml");
        }



        /// <summary>
        /// Event update
        /// </summary>
        /// <param name="oldEvent">old event</param>
        /// <param name="newEvent">new event</param>
        public void EventUpdate(Order oldEvent, Order newEvent)
        {
            oldEvent.start_date = newEvent.start_date;
            oldEvent.end_date = newEvent.end_date;
            oldEvent.description = newEvent.description;
            oldEvent.text = newEvent.text;
            oldEvent.pick_location = newEvent.pick_location;
            oldEvent.drop_location = newEvent.drop_location;
        }


        /// <summary>
        /// Create time select
        /// </summary>
        /// <param name="scheduler">scheduler</param>
        /// <param name="selected">selected</param>
        /// <returns>List of SelectListItemDHXScheduler.Skin</returns>
        private List<SelectListItem> _CreateTimeSelect(DHXScheduler scheduler, int? selected)
        {
            var opts = new List<SelectListItem>();
            for (var i = scheduler.Config.first_hour; i < scheduler.Config.last_hour; i++)
            {
                var value = string.Format("{0}:00", i < 10 ? "0" + i.ToString() : i.ToString());
                var item = new SelectListItem() { Text = value, Value = i.ToString() };
                if (selected != null && i == selected.Value)
                    item.Selected = true;
                opts.Add(item);
            }
            return opts;
        }
    }
}