/**
* Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
* Copyrights licensed under the New BSD License.
* See the accompanying LICENSE file for terms.
*/
//12:47 AM 9/20/2018
// Rajib Chy
using System;
namespace Sow.Framework.Security {
    public class ScheduleSettings : IScheduleSettings {
        public string TaskName { get; set; }
        public DateTime TriggerDateTime { get; set; }
        public string Description { get; set; }
        public string ActionPath { get; set; }
        public string Arguments { get; set; }
        public string StartIn { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}