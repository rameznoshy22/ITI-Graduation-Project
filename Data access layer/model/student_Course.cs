using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_access_layer.model
{
    public class student_Course
    {
        [Key]
        public int ID { get; set; }

        public int StudentID { get; set; }
        public int CourseID { get; set; }

        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;

        [Required]
        public string PaymentStatus { get; set; }

        // Navigation properties
        [ForeignKey(nameof(StudentID))]
        public virtual Student Student { get; set; }

        [ForeignKey(nameof(CourseID))]
        public virtual Course Course { get; set; }
    }


}
