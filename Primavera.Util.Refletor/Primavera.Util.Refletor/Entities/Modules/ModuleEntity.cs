using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primavera.Util.Refletor.Entities
{
    public class ModuleEntity
    {
        public List<TypeEntity> Types { get; set; }

        public ModuleEntity()
        {
            this.Types = new List<TypeEntity>();
        }
    }
}
