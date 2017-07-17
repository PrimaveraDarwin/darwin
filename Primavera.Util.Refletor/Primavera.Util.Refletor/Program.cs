using Primavera.Util.Refletor.Entities;
using Primavera.Util.Refletor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primavera.Util.Refletor
{
    class Program
    {
        static void Main(string[] args)
        {
            Decompile decompile = new Decompile();
            ModuleEntity moduleEntity= decompile.DecompileAssembly(@"C:\prj32_2012\Reflector\Assembly\VndBS100.dll");
            Console.ReadKey();
        }
    }
}
