/*
Date picker behavior
*/
//date format for inputs
scheduler.pickerDateFormat = "%d/%m/%Y";

function show_minical(id, start_date){
    var position = $("#" + id).get(0);

    if (scheduler.isCalendarVisible()) {
        setTimeout(function () {
            scheduler.destroyCalendar();
        }, 1);
    }else{
        scheduler.renderCalendar({
            position:id,
            date:scheduler._date,
            navigation:true,
            handler:function(date,calendar){
                $("#" + id).val(scheduler.date.date_to_str(scheduler.pickerDateFormat)(date));
                
                //check if 'To' date is later than 'From' date
                if (!areDatesCorrect()) {
                    showWarning();
                } else {
                    scheduler.updateFilters();
                }
                scheduler.destroyCalendar()
            }
        });
    }
    function areDatesCorrect() {
        var from = $("#DateFrom").val(),
            to = $("#DateTo").val();

        if (from && to) {
            //function to conver string to date object
            var converter = scheduler.date.str_to_date(scheduler.pickerDateFormat);
            from = converter(from);
            to = converter(to);
            if (from && to) {//if converted successfully
                if (from.getTime() > to.getTime())
                    //return false only if start date is later than end date, other cases are valid
                    return false;
            }

        }
        return true;
    }
    function showWarning() {
        $("#DateTo").val("");
        dhtmlx.message("Pick up date must be before Drop off date!");
    }
}



//define template for timeline units
function defineTemplates() {
    scheduler.config.month_day_min_height = 143;
    var label_template = function (key, label, obj) {
        if (obj.parent == true) {
            return "<div style=\"position:relative;top:15px;\">" +
            "<img src=\"" + obj.Photo + "\" alt=\"" + label + "\"></img><br/>" +
            "<div class=\"car_brand\">" + label + "</div><div class=\"car_price\">$" + obj.Price + "</div></div>" +
            "<div style=\"position:relative;top:15px;\"><img style=\"" + ((obj.open != true) ? ("padding-left: 110px;\"  src=\"" + "Content/expand.png")
                : ("padding-left: 105px;\" width='11' height='11' src=\"" + "Content/collapse.png")) + "\" alt=\"" + label + "\"></img></div><br/>";
        }
        else {
            return obj.name;
        }
    };
    var date_format = scheduler.date.date_to_str("%M %d");

    for (var i in scheduler.matrix) {
        scheduler.templates[i + '_scale_label'] = label_template;
        scheduler.templates[i + '_scale_date'] = date_format;
    }
    scheduler.config.hour_date = "%g:%i%a";
    scheduler.attachEvent("onTemplatesReady", function () {
        scheduler.templates['Orders_scale_date'] = scheduler.date.date_to_str(scheduler.config.hour_date);
    });
    scheduler.config.active_link_view = "Orders";
    scheduler.config.select = false;

    scheduler.templates.event_class = function (start, end, event) {
        if (event.shadow_event) return "virtual_event";
    };

    // add tooltip
    function short_date_template(start, end) {
        return scheduler.templates.event_header(start, end) + ", " + scheduler.templates.day_date(start);
    }

    function long_date_template(start) {
        return scheduler.templates.event_date(start) + " " + scheduler.templates.day_date(start);
    }

    function is_short_event(event) {
        return +scheduler.date.date_part(new Date(event.start_date)) == +scheduler.date.date_part(new Date(event.end_date))
    }

    scheduler.templates.tooltip_text = function (start, end, event) {
        var lines = [];
        var duration = scheduler.calculateDuration(event);
        var format = scheduler.date.date_to_str("%Y-%m-%d %H:%i");
        var car = cars[event.car_id];
        
        var duration = scheduler.calculateDuration(event);
        lines.push("<b>" + cars[event.car_id].label + "</b>, for <b>" + duration + "</b> hours");

        if (is_short_event(event)) {
            lines.push("Rented: " + short_date_template(start, end));
        } else {

            lines.push("From: " + long_date_template(start));
            lines.push("To: " + long_date_template(start));
        }

        var price = car.Price;
        lines.push("Price per hour: " + price + "$");
        sum = (Math.round((duration * price) * 100) / 100) + "$";
        lines.push("Total: <b>" + sum + "</b>");

        return lines.join("<br>");

    };

    scheduler.templates.event_bar_date = function (start, end, ev) {
        return "";
    };



    scheduler._split_events = function (evs) {
        var stack = [];

        var map_to = scheduler.matrix[scheduler.getState().mode].y_property;

        for (var i = 0; i < evs.length; i++) {
            stack.push(evs[i]);

            var shadow_ev = scheduler._lame_clone(evs[i]);
            var section = shadow_ev[map_to];
            var parent = getParentId(evs[i][map_to]);
            shadow_ev[map_to] = parent;
            if (cars[parent]) {
                shadow_ev._count = cars[parent].children.length;
                shadow_ev._sorder = Math.max(evs[i].car_number * 1 - 1, 0) || 0;
            }
            shadow_ev.shadow_event = true;
            stack.push(shadow_ev);
        }

        return stack;
    };

    var vis_evs = scheduler.get_visible_events;
    scheduler.get_visible_events = function (only_timed) {
        var evs = vis_evs.apply(this, arguments);
        var timeline = scheduler.matrix[scheduler.getState().mode];
        if (timeline) {

            evs = this._split_events(evs);
        }
        return evs;
    };

    var old_view_data = scheduler.render_view_data;
    scheduler.render_view_data = function (evs, hold) {
        if (scheduler.matrix[scheduler.getState().mode] && evs) {
            //render single event during dnd, restore flags that were calculated during full render
            evs = this._split_events(evs);
        }

        return old_view_data.apply(this, [evs, hold]);
    };

    scheduler.calculateDuration = function (event) {
        return Math.round((event.end_date - event.start_date) / (1000 * 60 * 60));
    };

    scheduler.templates.event_bar_text = function (start, end, event) {
        var time = scheduler.calculateDuration(event);

        if (scheduler.getState().mode == "month" && is_short_event(event)) {
            return cars[event.car_id].label + ", " + time + " h.";
        } else {
            return "Rented for " + time + " hours";
        }

    };
}

function afterInit() {

    // Live Update events for all users
    var hub = $.connection.schedulerHub;
    var uid = scheduler.uid();

    scheduler.dataProcessor.attachEvent("onLocalUpdate", function (data) {
        data.uid = uid;
        hub.server.send(JSON.stringify(data));
    });

    hub.client.addMessage = function (message) {
        var ev = JSON.parse(message);
        if (ev.uid == uid) return;
        var initial_open = true,
            parent = null;
        var matrix = scheduler.matrix[scheduler.getState().mode];
        if (matrix) {
            var map_to = matrix.y_property;
            parent = getParentId(ev.data[map_to]);
            for (var i = 0; i < matrix.y_unit_original.length; i++) {
                if (matrix.y_unit_original[i].key == parent && !matrix.y_unit_original[i].open) {
                    initial_open = false;
                    break;
                }
            }
            scheduler.openSection(parent);
        }


        scheduler.dataProcessor.applyChanges(ev);
        if (!initial_open) {
            scheduler.closeSection(parent);
        }
    };

    $.connection.hub.start();



    function isEditableSection(e) {
        var pos = scheduler.getActionData(e);
        if (pos.section && (!getParentId(pos.section) || getParentId(pos.section) == pos.section)) {
            return false;
        }
        return true;
    }
    scheduler.attachEvent("onClick", function (id, e) {
        return isEditableSection(e);
    });

    scheduler.attachEvent("onDblClick", function (id, e) {
        return isEditableSection(e);
    });

    scheduler.attachEvent("onBeforeDrag", function (id, mode, e) {
        return isEditableSection(e);
    });

    scheduler.attachEvent("onViewMoreClick", function (date) {
        scheduler.setCurrentView(date, "Orders");
        return false;
    });

    scheduler.attachEvent("onBeforeEventChanged", function (ev, dom_event) {
        if (isEditableSection(dom_event)) {
            var map_to = scheduler.matrix[scheduler.getState().mode].y_property;
            scheduler.updateCarBinding(ev, ev[map_to]);
            return true;
        }
        return false;
    });

    scheduler.attachEvent("onEventSave", function (id, ev, is_new) {
        if (scheduler.matrix[scheduler.getState().mode]) {
            var map_to = scheduler.matrix[scheduler.getState().mode].y_property;
            var parent = getParentId(ev[map_to]);
            scheduler.openSection(parent);
            scheduler.updateCarBinding(ev, ev[map_to]);
            var prev = scheduler.getEvent(id);
            var prev_section = prev[map_to];

            var close_prev_folder = false;
            if (prev && prev[map_to]) {
                close_prev_folder = true;
                var evs = scheduler.getEvents(scheduler.getState().min_date, scheduler.getState().max_date);
                for (var i = 0; i < evs.length; i++) {
                    if (evs[i].id != id && getParentId(evs[i]) == prev_section) {
                        close_prev_folder = false;
                    }
                }
            }
            if (close_prev_folder) {
                scheduler.closeSection(prev_section);
            }
        }
        return true;
    });



    // event sort by car
    scheduler.matrix["Orders"].sort = function (a, b) {
        if (a.shadow_event && b.shadow_event) {
            if (a.carChild_id > b.carChild_id) {
                return 1;
            }

            if (a.carChild_id < b.carChild_id) {
                return -1;
            }
        }
        return +a.start_date > +b.start_date ? 1 : -1;
    }

    scheduler.initCarsList(scheduler.serverList(scheduler.config.serverLists["Orders"]));

    scheduler.attachEvent("onEventLoading", function (event) {
        scheduler.initialCarBinding(event);
        return true;
    });
    scheduler.updateResetButton();
};

scheduler.generateCarKey = function (car) {
    if (car.Count) {
        return car.id;
    } else {
        return scheduler.subsectionCarKey(car.carId, car.carNumber);
    }
};
scheduler.subsectionCarKey = function (car_id, index) {
    return [car_id, index].join(";");
};

scheduler.parseCarId = function (car_id) {
    return (car_id || "").split(";");
};
scheduler.updateCarBinding = function (event, car_id) {
    var car_binding = this.parseCarId(car_id);
    event.car_id = car_binding[0];
    event.car_number = car_binding[1];
};
scheduler.initialCarBinding = function (event) {
    event.section_id = scheduler.subsectionCarKey(event.car_id, event.car_number);
};

scheduler.initCarsList = function (cars) {
    var res = [];
    var lightbox_options = [];
    var search_hash = window.cars  = {};
    var parent_hash = window.sections = {};

    for (var index = 0; index < cars.length; index++) {
        var car = cars[index];
        res.push(car);
        car.key = scheduler.generateCarKey(car);
        search_hash[car.key] = car;
        parent_hash[car.key] = undefined;
        car.label = car.Brand;
        car.children = [];
        car.parent = true;
        for (var i = 1; i <= car.Count; i++) {
            
            var child = {
                label: "Car " + i,
                carNumber: i,
                carId: car.id
            };
            child.key = scheduler.generateCarKey(child);
            car.children.push(child);
            lightbox_options.push({
                key: child.key,
                label : car.Brand + " #" + i
            });
            search_hash[child.key] = car;
            parent_hash[child.key] = car.key;
        }
    }
    scheduler.updateCollection(scheduler.config.serverLists["Orders"], res.slice());
    scheduler.updateCollection("SelectOptions", lightbox_options);
};



// get parent event id
function getParentId(id) {
    return window.sections[id];
}

function getCar(key) {
    return window.cars[key];
}

scheduler.updateFilters = function () {
    var vals = $("#myForm").serialize();
    $.ajax({
        type:"POST",
        url: "Home/FilterCars",
        data: vals,
        success: function (resp) {
            if (!resp.length) {
                dhtmlx.message("Nothing was found on your request");
                if(scheduler.hasFilters())
                    scheduler.resetForm();
            }else{
                scheduler.initCarsList(resp);
            }
        }
    });
    scheduler.updateResetButton();
};
scheduler.resetForm = function resetForm() {
    $('#Type').val("");
    $('#PriceRange').val("");
    $('#DateFrom').val("");
    $('#DateTo').val("");
    scheduler.updateFilters();
};
scheduler.hasFilters = function(){
    var valArray = $("#myForm").serializeArray();
    var hasValues = false;
    for (var i = 0; i < valArray.length; i++) {
        if (valArray[i].value && valArray[i].value != "false" && valArray[i].value != "true" && valArray[i].value != "0") {
            hasValues = true;
            break;
        }
    }
    return hasValues;
};
scheduler.updateResetButton = function () {

    if (!scheduler.hasFilters()) {
        $("#resetFilter").hide();
    } else {
        $("#resetFilter").show();
    }
};