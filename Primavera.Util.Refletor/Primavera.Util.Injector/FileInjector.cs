using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Primavera.Util.Injector.Helpers;
using Primavera.Util.Refletor.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Common;

namespace Primavera.Util.Injector
{
    public class FileInjector
    {
        public void Inject(MethodEntity methodEntity, string preLine, string posLine)
        {
            MethodLocation location = methodEntity.Location;

            InsertLineAtBegining(location.Url, "using Primavera.Extensibility.Constants.ExtensibilityEvents;");
            InsertLineAtBegining(location.Url, "using Primavera.Extensibility.Constants.ExtensibilityService;");

            
            if (!FileHelper.ExistLine(location.Url, location.StartLine, location.EndLine, posLine))
            {
                CheckoutFileWithTFS(location.Url);
                this.InsertPosLine(methodEntity, posLine);
            }

            if (!FileHelper.ExistLine(location.Url, location.StartLine, location.EndLine, preLine))
            {
                CheckoutFileWithTFS(location.Url);
                this.InsertPreLine(methodEntity, preLine);
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
                string filepath = methodEntity.Location.Url;

                int lineToInsert = methodEntity.Exceptions.First().TryStart.StartLine +1;
                int columnToInsert = methodEntity.Exceptions.First().TryStart.StartColumn * 4;
                string methodSignature;
                var argsNames = MethodHelper.MethodSignature(methodEntity);

                if (!argsNames.IsNullOrEmpty())
                    methodSignature = "".PadRight(columnToInsert) + text + ", " + argsNames + ");" + Environment.NewLine;
                else
                    methodSignature = "".PadRight(columnToInsert) + text + argsNames + ");" + Environment.NewLine;

                FileHelper.InsertLine(filepath, methodSignature, lineToInsert + 1);
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
                bool review = false;
                string inFrontOfReturn = string.Empty;
                string fullLine = string.Empty;

                string filepath = methodEntity.Location.Url;


                MethodException exception = methodEntity.Exceptions.First();
                int lineToInsert = exception.TryEnd.StartLine;

                var lines = File.ReadAllLines(filepath);
                for (int i = lineToInsert; i > exception.TryStart.StartLine; i--)
                {
                    if (Regex.Match(lines[i], "\\s*return\\s*\\(").Success || Regex.Match(lines[i], "\\s*return\\s*m_objErpBSO.").Success)
                    {
                        lineToInsert = i;
                        inFrontOfReturn = lines[i].Replace("return", string.Empty);
                        fullLine = lines[i];

                        review = true;

                        break;
                    }
                    else if (Regex.Match(lines[i], "\\s*return\\s*\\;").Success)
                    {
                        lineToInsert = i;

                        break;
                    }
                }

                int columnToInsert = exception.TryEnd.StartColumn * 4;
                string methodSignature;
                var argsNames = MethodHelper.MethodSignature(methodEntity);

                if (!argsNames.IsNullOrEmpty())
                    methodSignature = "".PadRight(columnToInsert) + text + ", " + argsNames + ");" + Environment.NewLine;
                else
                    methodSignature = "".PadRight(columnToInsert) + text + argsNames + ");" + Environment.NewLine;

                if (review)
                {
                    //methodSignature = "EXTENSIBILITY_REVIEW" + methodSignature;
                    FileHelper.InsertLine(filepath, "var obj = " + inFrontOfReturn, lineToInsert-1);
                    FileHelper.InsertLine(filepath, methodSignature, lineToInsert);
                    FileHelper.ReplaceText(filepath, fullLine, "return obj;");
                }
                else
                {
                    FileHelper.InsertLine(filepath, methodSignature, lineToInsert);
                }
                
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

        private void InsertLineAtBegining(string filePath, string text)
        {
            if (!File.ReadAllText(filePath).Contains(text))
            {
                CheckoutFileWithTFS(filePath);
                FileHelper.InsertLine(filePath, text, 0);
            }
        }
    }
}
