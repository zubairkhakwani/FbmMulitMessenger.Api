namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class Role
    {
        public int Id { get; set; }
        public required string Name { get; set; }

        public DateTime CreatedAt { get; set; }

        //Navigation Property 
        public List<User> Users { get; set; } = new List<User>();
    }
}
