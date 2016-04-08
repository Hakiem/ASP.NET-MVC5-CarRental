/*
@license
dhtmlxScheduler.Net v.3.3.16 Professional Evaluation

This software is covered by DHTMLX Evaluation License. Contact sales@dhtmlx.com to get Commercial or Enterprise license. Usage without proper license is prohibited.

(c) Dinamenta, UAB.
*/
Scheduler.plugin(function(e){!function(){e.config.all_timed="short";var t=function(e){return!((e.end_date-e.start_date)/36e5>=24)};e._safe_copy=function(t){var a=null,d=null;return t.event_pid&&(a=e.getEvent(t.event_pid)),a&&a.isPrototypeOf(t)?(d=e._copy_event(t),delete d.event_length,delete d.event_pid,delete d.rec_pattern,delete d.rec_type):d=e._lame_clone(t),d};var a=e._pre_render_events_line;e._pre_render_events_line=function(d,n){function _(e){var t=i(e.start_date);return+e.end_date>+t}function i(t){
var a=e.date.add(t,1,"day");return a=e.date.date_part(a)}function r(t,a){var d=e.date.date_part(new Date(t));return d.setHours(a),d}if(!this.config.all_timed)return a.call(this,d,n);for(var s=0;s<d.length;s++){var l=d[s];if(!l._timed)if("short"!=this.config.all_timed||t(l)){var c=this._safe_copy(l);c.start_date=new Date(c.start_date),_(l)?(c.end_date=i(c.start_date),24!=this.config.last_hour&&(c.end_date=r(c.start_date,this.config.last_hour))):c.end_date=new Date(l.end_date);var o=!1;c.start_date<this._max_date&&c.end_date>this._min_date&&c.start_date<c.end_date&&(d[s]=c,
o=!0);var v=this._safe_copy(l);if(v.end_date=new Date(v.end_date),v.start_date=v.start_date<this._min_date?r(this._min_date,this.config.first_hour):r(i(l.start_date),this.config.first_hour),v.start_date<this._max_date&&v.start_date<v.end_date){if(!o){d[s--]=v;continue}d.splice(s+1,0,v)}}else d.splice(s--,1)}var h="move"==this._drag_mode?!1:n;return a.call(this,d,h)};var d=e.get_visible_events;e.get_visible_events=function(e){return this.config.all_timed&&this.config.multi_day?d.call(this,!1):d.call(this,e);

},e.attachEvent("onBeforeViewChange",function(t,a,d,n){return e._allow_dnd="day"==d||"week"==d,!0}),e._is_main_area_event=function(e){return!!(e._timed||this.config.all_timed===!0||"short"==this.config.all_timed&&t(e))};var n=e.updateEvent;e.updateEvent=function(t){var a,d=e.config.all_timed&&!(e.isOneDayEvent(e._events[t])||e.getState().drag_id);d&&(a=e.config.update_render,e.config.update_render=!0),n.apply(e,arguments),d&&(e.config.update_render=a)}}()});