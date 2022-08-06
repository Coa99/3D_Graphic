using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PZ3.Models
{
    public class SwitchEntity : PowerEntity
    {
        public long ID { get; set; }
        public string Name { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        public string Status { get; set; }

        public SwitchEntity()
        {

        }
    }
}
