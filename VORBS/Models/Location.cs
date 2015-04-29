﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.ComponentModel.DataAnnotations.Schema;

namespace VORBS.Models
{
    public class Location
    {
        public int ID { get; set; }

        public string Name { get; set; }
        public virtual ICollection<Room> Rooms { get; set; }
    }
}