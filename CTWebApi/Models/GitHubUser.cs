namespace CTWebApi.Models
{
    public class GitHubUser
    {
        public string? name { get; set; }
        public string? login { get; set; }
        public string? company { get; set; }
        public int followers { get; set; }
        public int public_repos { get; set; }
        public int flowers_per_p_repos
        {
            get
            {
                if (public_repos == 0)
                {
                    return 0;
                }
                else return followers / public_repos;
            }
        }
    }
}
