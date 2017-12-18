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


            // 1st pass: Comment 2nd Pass
            //InsertLineAtBegining(location.Url, "using Primavera.Extensibility.Constants.ExtensibilityEvents;");
            //InsertLineAtBegining(location.Url, "using Primavera.Extensibility.Constants.ExtensibilityService;");

            //if (!FileHelper.ExistLine(location.Url, location.StartLine, location.EndLine, posLine))
            //{
            //    CheckoutFileWithTFS(location.Url);
            //    this.InsertPosLine(methodEntity, posLine);
            //}

            //if (!FileHelper.ExistLine(location.Url, location.StartLine, location.EndLine, preLine))
            //{
            //    CheckoutFileWithTFS(location.Url);
            //    this.InsertPreLine(methodEntity, preLine);
            //}

            // 2nd Pass: Comment 1nd Pass
            InsertExtensibilityTypeString(location.Url);
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

                int lineToInsert = methodEntity.Exceptions.First().TryStart.StartLine + 1;
                int columnToInsert = methodEntity.Exceptions.First().TryStart.StartColumn * 4;
                string methodSignature;
                var argsNames = MethodHelper.MethodSignature(methodEntity);

                if (!argsNames.IsNullOrEmpty())
                    methodSignature = "".PadRight(columnToInsert) + text + ", " + argsNames + ");" + Environment.NewLine;
                else
                    methodSignature = "".PadRight(columnToInsert) + text + argsNames + ");" + Environment.NewLine;

                FileHelper.InsertLine(filepath, "".PadRight(columnToInsert) + "// Extensibility Service Event",
                    lineToInsert + 1);
                FileHelper.InsertLine(filepath, methodSignature, lineToInsert + 2);
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
                    if (Regex.Replace(lines[i], @"\s+", "").StartsWith("}"))
                        continue;

                    if (Regex.Replace(lines[i], @"\s+", "").StartsWith("catch"))
                        continue;

                    if (Regex.Replace(lines[i], @"\s+", "").StartsWith("{"))
                        continue;

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
                    else if (!string.IsNullOrEmpty(Regex.Replace(lines[i], @"\s+", "")) && !Regex.Replace(lines[i], @"\s+", "").StartsWith("return"))
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
                    FileHelper.InsertLine(filepath, "var obj = " + inFrontOfReturn, lineToInsert);
                    FileHelper.InsertLine(filepath, "".PadRight(columnToInsert) + "// Extensibility Service Event", lineToInsert + 1);
                    FileHelper.InsertLine(filepath, methodSignature, lineToInsert + 2);
                    FileHelper.ReplaceText(filepath, fullLine, "return obj;");
                }
                else
                {
                    FileHelper.InsertLine(filepath, "".PadRight(columnToInsert) + "// Extensibility Service Event", lineToInsert);
                    FileHelper.InsertLine(filepath, methodSignature, lineToInsert + 1);
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

        private void InsertExtensibilityTypeString(string filePath)
        {
            var className = "\\s*public\\s+class\\s+(\\S+)";
            var findOpening = "\\s*\\{";
            var openingLine = -1;
            var openingLine2 = -1;
            var classNameValue = string.Empty;

            var lines = File.ReadAllLines(filePath);
            for (int i = 0; i < lines.Length; i++)
            {
                if (openingLine == -1)
                {
                    if (Regex.Match(lines[i], findOpening).Success)
                        openingLine = i;
                }
                else
                {
                    if (string.IsNullOrEmpty(classNameValue))
                    {
                        if (Regex.Match(lines[i], className).Success)
                        {
                            classNameValue = Regex.Match(lines[i], className).Groups[1].Value;
                        }
                    }
                    else
                    {
                        if (openingLine2 == -1)
                        {
                            if (Regex.Match(lines[i], findOpening).Success)
                                openingLine2 = i;
                        }
                        else
                        {
                            //Inject here!
                            var injectText = $"public string ExtensibilityTypeName = \"{classNameValue}\";";

                            if (!File.ReadAllText(filePath).Contains(injectText))
                            {
                                CheckoutFileWithTFS(filePath);
                                FileHelper.InsertLine(filePath, "\n"+injectText, openingLine2+1);
                            }

                            break;
                        }
                    }
                }
            }
        }
    }
}
