using Primavera.Util.Refletor.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primavera.Util.Injector.Helpers
{
    public static class MethodHelper
    {
        /// <summary>
        /// Methods the signature.
        /// </summary>
        /// <param name="methodEntity">The method entity.</param>
        /// <returns></returns>
        public static string MethodSignature(MethodEntity methodEntity)
        {
            string methodParameters = string.Empty;

            if(methodEntity.Parameters.Count > 0)
            {
                foreach (MethodParameter parameter in methodEntity.Parameters)
                {
                    methodParameters += parameter.Name + ", ";
                }

                return methodParameters.Substring(0, methodParameters.Length - 2);
            }

            return string.Empty;
        }
    }
}
