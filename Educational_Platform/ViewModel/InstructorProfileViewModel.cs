using System.ComponentModel.DataAnnotations;

namespace Educational_Platform.ViewModel
{
    public class InstructorProfileViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Subjects")]
        public string Subjects { get; set; }

        [Display(Name = "Qualifications")]
        public string Qualifications { get; set; }

        [Display(Name = "Biography")]
        [DataType(DataType.MultilineText)]
        public string Bio { get; set; }

        [Display(Name = "Profile Picture")]
        public IFormFile ProfilePictureFile { get; set; }

        public string CurrentProfilePicture { get; set; }
    }
}