﻿using Microsoft.SemanticKernel;
using System;
using System.ComponentModel;

namespace ChatBots.Plugins;

class ClinicPlugins
{
    [KernelFunction("get_day")]
    [Description("Gets today's day of the week")]
    [return: Description("Will tell which day of the week is today; when composing the answer where today's day of week for reference is required use this")]
    public string GetDayOfWeek()
    {
        return DateTime.Now.DayOfWeek.ToString();
    }

    [KernelFunction("make_appointment")]
    [Description("Makes an appointment with clinic; one can make an appointment for future days as well. Before making an appointment; ask the user his mobile number and ensure its valid mobile number, valid mobile numbers are 11 digits and start with 03, appointments can only be made for evenings")]
    [return: Description("Will tell if appointment of given day of week is made or not; if slot/time is not available it will tell 'Not Available', suggest next day if slot/time is not available")]
    public string MakeAppointmentAsync(string phoneNumber, string dayOfWeek)
    {
        if (phoneNumber is null || !phoneNumber.StartsWith("03") || phoneNumber.Length < 11)
            return "I need a valid mobile phone number";
        if (dayOfWeek is null)
            return "I need a valid day";
        else
            return $"Appointment for {phoneNumber} is made for 6PM on {dayOfWeek}";
    }
}
