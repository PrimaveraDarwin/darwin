using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Primavera.Util.Injector.Helpers;
using Primavera.Util.Refletor.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primavera.Util.Injector
{
    public class FileInjector
    {
        public void Inject(MethodEntity methodEntity, string preLine, string posLine)
        {
            MethodLocation location = methodEntity.Location;

            if (!FileHelper.ExistLine(location.Url, location.StartLine, location.EndLine, preLine))
            {
                CheckoutFileWithTFS(location.Url);
                this.InsertPreLine(methodEntity, preLine);
            }
            if (!FileHelper.ExistLine(location.Url, location.StartLine, location.EndLine, posLine))
            {
                CheckoutFileWithTFS(location.Url);
                this.InsertPosLine(methodEntity, posLine);
            }
        }

        /// <summary>
        /// Inserts the pre line.
        /// </summary>
        private void InsertPreLine(MethodEntity methodEntity, string text)
        {
            List<MethodException> exceptions = methodEntity.Exceptions;

            if (exceptions.Count > 0)
            {
                string file = methodEntity.Location.Url;
                int lineToInsert = methodEntity.Exceptions.First().TryStart.StartLine;
                int columnToInsert = methodEntity.Exceptions.First().TryStart.StartColumn * 4;
                string methodSignature = "".PadRight(columnToInsert) + text + MethodHelper.MethodSignature(methodEntity) + ";" + Environment.NewLine;

                FileHelper.InsertLine(file, methodSignature, lineToInsert + 1);
            }
        }

        /// <summary>
        /// Inserts the position line.
        /// </summary>
        /// <param name="methodEntity">The method entity.</param>
        private void InsertPosLine(MethodEntity methodEntity, string text)
        {
            List<MethodException> exceptions = methodEntity.Exceptions;

            if (exceptions.Count > 0)
            {
                string filepath = methodEntity.Location.Url;
                MethodException exception = methodEntity.Exceptions.First();
                int lineToInsert = exception.TryEnd.StartLine;
                int columnToInsert = exception.TryEnd.StartColumn * 4;
                string methodSignature = "".PadRight(columnToInsert) + text + MethodHelper.MethodSignature(methodEntity) + ";" + Environment.NewLine;

                FileHelper.InsertLine(filepath, methodSignature, lineToInsert);
            }
        }

        private void CheckoutFileWithTFS(string filepath)
        {
            FileInfo fileInfo = new FileInfo(filepath);

            if (fileInfo.IsReadOnly)
            {
                var workspaceInfo = Workstation.Current.GetLocalWorkspaceInfo(filepath);
                using (var server = new TfsTeamProjectCollection(workspaceInfo.ServerUri))
                {
                    var workspace = workspaceInfo.GetWorkspace(server);
                    workspace.PendEdit(filepath);
                }
            }
        }
    }
}
