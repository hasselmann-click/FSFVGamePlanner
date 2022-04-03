using FSFV.Gameplanner.Common.Dto;

namespace FSFV.Gameplanner.Common;

public class Group
{
    public string Name { get; set; }
    public GroupTypeDto Type { get; set; }

    public override bool Equals(object obj)
    {
        return obj is Group grouping &&
               Name.Equals(grouping.Name);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
