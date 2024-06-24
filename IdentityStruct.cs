namespace SearchQueryKeywordFixes
{
    public class IdentityStruct
    {
        public string grant_type { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string scope { get; set; }

        public override string ToString()
        {
            return $"grant_type={grant_type}&username={username}&password={password}&scope={scope}";
        }

    }
}
