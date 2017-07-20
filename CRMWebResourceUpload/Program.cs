using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.IO;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Crm.Sdk.Messages;

namespace CRMWebResourceUpload
{
    class Program
    {
        // This is essentailly the contents of the web resource type option set. It is used to map file extensions to resource types
        readonly static Dictionary<string, int> webResourceType = new Dictionary<string, int>()
        {
            { ".html", 1 },
            { ".ico", 10 },
            { ".css", 2 },
            { ".js", 3 },
            { ".xml", 4 },
            { ".png", 5 },
            { ".jpg", 6 },
            { ".gif", 7 },
            { ".xap", 8 },
            { ".xsl", 9 },
        };

        static void Main(string[] args)
        {
            string connectionString = args[0];
            string solutionName = args[1];
            string source = args[2];

            Console.WriteLine("Connecting");
            CrmServiceClient service = new CrmServiceClient(connectionString);
            EntityReference solution = GetSolution(service, solutionName);

            // Get the two lists of files to compare
            Console.WriteLine("Getting all current web resources");
            List<Entity> currentResources = GetSolutionWebResources(service, solution);
            Console.WriteLine("Getting all local web resources");
            List<string> localFiles = GetLocalFiles(source, source);

            List<Guid> updatedWebResourceIds = new List<Guid>();

            // Loop through all of the local files
            foreach (var file in localFiles)
            {
                if (!webResourceType.ContainsKey(Path.GetExtension(file)))
                {
                    Console.WriteLine($"File extension {Path.GetExtension(file)} isn't valid");
                }
                var currentFileContents = File.ReadAllBytes(Path.Combine(source, file));
                var cr = currentResources.FirstOrDefault(e => (string)e["name"] == file);
                if (cr == null)
                {
                    Console.WriteLine($"Creating: {file}");
                    Entity newResource = new Entity("webresource");
                    newResource["content"] = Convert.ToBase64String(currentFileContents);
                    newResource["name"] = file;
                    newResource["displayname"] = file;
                    newResource["webresourcetype"] = new OptionSetValue(webResourceType[Path.GetExtension(file)]);

                    // Need to do this funky workaround so that I can get it in the right solution
                    CreateRequest create = new CreateRequest()
                    {
                        Target = newResource
                    };
                    create.Parameters.Add("SolutionUniqueName", solutionName);

                    service.Execute(create);
                }
                else
                {
                    if (Convert.ToBase64String(currentFileContents) == (string)cr["content"])
                    {
                        Console.WriteLine($"Skipping {file} contents are the same");
                    }
                    else
                    {
                        Console.WriteLine($"Updating {file}");
                        cr["content"] = Convert.ToBase64String(currentFileContents);
                        service.Update(cr);
                        updatedWebResourceIds.Add(cr.Id);
                        
                    }
                }
            }

            if (updatedWebResourceIds.Count > 0)
            {
                Console.WriteLine("Publishing");

                // Build the publish request xml
                StringBuilder publishxml = new StringBuilder();
                publishxml.AppendLine("<importexportxml>");
                publishxml.AppendLine("<webresources>");
                foreach (var i in updatedWebResourceIds)
                {
                    publishxml.AppendLine($"<webresource>{i.ToString()}</webresource>");
                }
                publishxml.AppendLine("</webresources>");
                publishxml.AppendLine("</importexportxml>");

                // Execute the publish request
                PublishXmlRequest publishRequest = new PublishXmlRequest() { ParameterXml = publishxml.ToString() };
                service.Execute(publishRequest);
            }
            else
            {
                Console.WriteLine("Skipping publish because no resources have changed");
            }

            Console.WriteLine("All done");
#if DEBUG
            Console.ReadLine();
#endif
        }

        /// <summary>
        /// Get a reference to a solution
        /// </summary>
        /// <param name="service">An instance of the organization service</param>
        /// <param name="solutionName">The unique name of the service to get</param>
        /// <returns></returns>
        static EntityReference GetSolution(IOrganizationService service, string solutionName)
        {
            QueryExpression solutionQuery = new QueryExpression("solution");
            solutionQuery.Criteria.AddCondition("uniquename", ConditionOperator.Equal, solutionName);
            solutionQuery.ColumnSet = new ColumnSet("version", "uniquename", "description");

            EntityCollection solutionResult = service.RetrieveMultiple(solutionQuery);
            if (solutionResult.Entities.Count == 0)
            {
                Console.WriteLine("Solution not found");
                Environment.Exit(1);
            }

            return solutionResult.Entities[0].ToEntityReference();
        }

        /// <summary>
        /// Get all the web resources for a given solution
        /// </summary>
        /// <param name="service">An instance of the organization service</param>
        /// <param name="solution">A reference to the solution to query for</param>
        /// <returns></returns>
        static List<Entity> GetSolutionWebResources(IOrganizationService service, EntityReference solution)
        {
            QueryExpression webResourceQuery = new QueryExpression("webresource");
            webResourceQuery.ColumnSet = new ColumnSet("content", "webresourcetype", "name", "description", "displayname");

            LinkEntity solutionLink = new LinkEntity("webresource", "solutioncomponent", "webresourceid", "objectid", JoinOperator.Inner);
            solutionLink.LinkCriteria.AddCondition("solutionid", ConditionOperator.Equal, solution.Id);
            solutionLink.EntityAlias = "solutioncomponent";

            webResourceQuery.LinkEntities.Add(solutionLink);

            return service.RetrieveMultiple(webResourceQuery).Entities.ToList();
        }
        /// <summary>
        /// Gets all of the files in a path and walks down the tree of directories
        /// </summary>
        /// <param name="path">path to start at</param>
        /// <param name="fullPath">full path (used in recursion)</param>
        /// <param name="firstRun">whether or not this is the first run (used in recursion)</param>
        /// <returns></returns>
        static List<string> GetLocalFiles(string path, string fullPath, bool firstRun = true)
        {
            List<string> localFiles = new List<string>();
            if (firstRun)
            {
                localFiles.AddRange(Directory.GetFiles(path));
            }
            else
            {
                localFiles.AddRange(Directory.GetFiles(path).Select(f => Path.Combine(path, f)));
            }
            foreach (var dir in Directory.GetDirectories(path))
            {
                if (path != ".")
                {
                    localFiles.AddRange(GetLocalFiles(Path.Combine(path, dir), fullPath, false));
                }
            }
            List<string> output = new List<string>();
            foreach (var i in localFiles)
            {
                output.Add(i.Replace(fullPath + "\\", ""));
            }

            return output;
        }
    }
}
