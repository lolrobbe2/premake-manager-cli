using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src.workspace
{
    internal struct Project
    {
        public string name;
        public string location;
        public string language;
    }

    internal class ProjectBuilder
    {

        private Project project;
        public ProjectBuilder SetName(string name)
        {
            project.name = name;
            return this;
        }

        public ProjectBuilder SetLocation(string location)
        {
            project.location = location;
            return this;
        }

        public ProjectBuilder SetLanguage(string language)
        {
            project.language = language;
            return this;
        }

        public Project Build()
        {
            return project;
        }
    }
}
