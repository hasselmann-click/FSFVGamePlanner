namespace FSFV.Gameplanner.Common.Dto
{
    public class Group
    {
        public int GroupingID { get; set; }
        public int Priority { get; set; }
        public GroupType Type { get; set; }
        public Group SubGroup { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Group grouping &&
                   GroupingID == grouping.GroupingID;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
