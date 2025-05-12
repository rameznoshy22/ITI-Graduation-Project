using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_access_layer.model
{
    // Instructor.cs
    public class Instructor
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [StringLength(255)]
        public string Email { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(255)]
        public string? Photo { get; set; }

        public string? Subjects { get; set; }
        public string? Qualifications { get; set; }
        public string? Bio { get; set; }
        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; }

        // Navigation properties

        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }




}