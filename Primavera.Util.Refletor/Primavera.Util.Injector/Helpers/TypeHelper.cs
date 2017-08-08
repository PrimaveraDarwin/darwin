using Primavera.Util.Refletor.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primavera.Util.Injector.Helpers
{
    public static class TypeHelper
    {
        public static bool HasPublicMethods(TypeEntity typeEntity)
        {
            foreach (MethodEntity methodEntity in typeEntity.Methods)
            {
                if (methodEntity.IsPublic)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
