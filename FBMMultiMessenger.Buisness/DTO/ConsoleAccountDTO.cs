using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.DTO
{
    //This class is responsible for sending account details to our console app.
    public class ConsoleAccountDTO
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Cookie { get; set; }
        public bool IsUpdateRequest { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
