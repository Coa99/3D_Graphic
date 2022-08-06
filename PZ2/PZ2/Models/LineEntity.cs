using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PZ3.Models
{
    public class LineEntity : PowerEntity
    {

        public long ID { get; set; }
        public string Name { get; set; }
        public long StartNodeID { get; set; }
        public long EndNodeID { get; set; }
        public string ConductorMaterial { get; set; }
        public double Resistance { get; set; }
        public List<PointEntity> Vertices { get; set; }

        public LineEntity()
        {

        }


    }
}
