namespace FSFV.Gameplanner.Common.Dto
{
    public class GroupTypeDto
    {

        public const string FileKey = "GroupTypes";

        public string Name { get; set; }
        public int MinDurationMinutes { get; set; }
        public int FinalsDays { get; set; }
        public string RequiredPitchName { get; set; }
        /// <summary>
        /// Priority when to play groups of this type. The higher the earlier.
        /// </summary>
        public int Priority { get; set; }
        /// <summary>
        /// The number of the first game day to consider.
        /// </summary>
        public int FixtureStart { get; set; }

    }
}
