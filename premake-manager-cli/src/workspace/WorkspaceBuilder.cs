using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#nullable enable
namespace src.workspace
{
    internal struct Workspace
    {
        public Workspace()
        {
            name = string.Empty;
            configurations = [];
            projects = [];
        }
        public string name;
        public IList<string> configurations;
        public IList<Project> projects;
    }
    internal class WorkspaceBuilder
    {
        private IList<Workspace> workspaces = new List<Workspace>();
        private Workspace workspace;
        public WorkspaceBuilder StartWorkspace() 
        {
            workspace = new Workspace();
            return this;
        }
        public WorkspaceBuilder SetName(string name)
        {
            workspace.name = name;
            return this;
        }
        public WorkspaceBuilder AddConfiguration(string config)
        {
            workspace.configurations.Add(config);
            return this;
        }

        public WorkspaceBuilder AddProject(Project project)
        {
            workspace.projects.Add(project);
            return this;
        }
        public WorkspaceBuilder EndWorkspace()
        {
            workspaces.Add(workspace);
            return this;
        }

        public void Build()
        {
            //TODO write to file
        }
        public int workspaceCount { get => workspaces.Count; }
        public int currentProjectCount { get => workspace.projects.Count; }
    }
}
