using Primavera.Util.Injector.Helpers;
using Primavera.Util.Refletor.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primavera.Util.Injector
{
    public class FileInjector
    {
        public void InjectPreLineOnActualiza(MethodEntity methodEntity)
        {
            if(methodEntity.Name.ToLower() == "actualiza")
            { 
                if(!FileHelper.ExistLine(methodEntity.Location.Url, methodEntity.Location.StartLine, methodEntity.Location.EndLine, "MotorLE.Extensibility.AntesActualizar"))
                {
                    this.InsertPreLine(methodEntity);
                }
            }
        }

        /// <summary>
        /// Inserts the pre line.
        /// </summary>
        private void InsertPreLine(MethodEntity methodEntity)
        {
            string padRight = string.Empty;

            int lineToInsert = methodEntity.Exceptions.First().TryStart.StartLine;
            int columnToInsert = methodEntity.Exceptions.First().TryStart.StartColumn * 4;
            string methodSignature = padRight.PadRight(columnToInsert) + "// MotorLE.Extensibility.AntesActualizar" + MethodHelper.MethodSignature(methodEntity) + ";" + Environment.NewLine;

            FileHelper.InsertLine(methodEntity.Location.Url, methodSignature, lineToInsert + 1);
        }

        /// <summary>
        /// Inserts the position line.
        /// </summary>
        /// <param name="methodEntity">The method entity.</param>
        private void InsertPosLine(MethodEntity methodEntity)
        {
        }

    }
}
