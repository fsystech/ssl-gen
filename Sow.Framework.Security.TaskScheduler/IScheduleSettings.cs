/**
* Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
* Copyrights licensed under the New BSD License.
* See the accompanying LICENSE file for terms.
*/
//12:47 AM 9/20/2018
// Rajib Chy
using System;
namespace Sow.Framework.Security {
	public interface IScheduleSettings {
		string TaskName { get; set; }
		DateTime TriggerDateTime { get; set; }
		string Description { get; set; }
		string ActionPath { get; set; }
		string Arguments { get; set; }
		string StartIn { get; set; }
		string UserName { get; set; }
		string Password { get; set; }
	}
}