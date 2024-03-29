
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDP_Project_Backend.Models
{
    public class Ticket
    {
        public int Id { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime Date { get; set; }

        public string IssueType { get; set; }

        [MaxLength(300)]
        public string Complaint { get; set; }

        public string Contact { get; set; }

		// Navigation property to represent the one-to-many relationship
		public User? User { get; set; }
        public int UserId { get; set; }

    }
}
