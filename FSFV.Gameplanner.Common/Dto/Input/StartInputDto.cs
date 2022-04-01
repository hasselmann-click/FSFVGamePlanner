using System.Collections.Generic;

namespace FSFV.Gameplanner.Common.Dto.Input
{
    public class StartInputDto
    {
        public string season { get; set; }

        public IList<DefinitionTupleDto> competitions { get; set; }

        public IList<DefinitionTupleDto> leagues { get; set; }

        public IList<GroupingDto> groupings { get; set; }

    }

    public class ContestDto
    {
        public string machineName { get; set; }

        public int? group { get; set; }

    }

    public class TeamDto
    {
        public string name { get; set; }

        public bool hasZK { get; set; }

        public IList<ContestDto> contests { get; set; }
    }

    public class GroupingDto
    {

        public string machineName { get; set; }

        public IList<TeamDto> teams { get; set; }

    }

    public class DefinitionTupleDto
    {
        public string name { get; set; }

        public string machineName { get; set; }
    }
}
