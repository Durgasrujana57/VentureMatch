import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

// Common skills suggestions
const COMMON_SKILLS = [
  'JavaScript', 'TypeScript', 'Angular', 'React', 'Vue', 'Node.js',
  'Python', 'Java', 'C#', 'PHP', 'Ruby', 'Go', 'Rust',
  'HTML', 'CSS', 'SCSS', 'Tailwind', 'Bootstrap',
  'MongoDB', 'PostgreSQL', 'MySQL', 'Firebase',
  'AWS', 'Azure', 'GCP', 'Docker', 'Kubernetes',
  'Git', 'GitHub', 'GitLab', 'CI/CD', 'Jenkins'
];

// Common interests suggestions
const COMMON_INTERESTS = [
  'AI', 'Machine Learning', 'Deep Learning', 'Data Science',
  'Web Development', 'Mobile Development', 'Game Development',
  'Startups', 'Entrepreneurship', 'Business', 'Marketing',
  'Design', 'UI/UX', 'Product Management',
  'Open Source', 'Cloud Computing', 'DevOps',
  'Blockchain', 'Cryptocurrency', 'NFT',
  'IoT', 'Robotics', 'AR/VR', 'Cybersecurity'
];

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent implements OnInit {
  registerForm!: FormGroup;
  loading = false;
  submitted = false;
  errorMessage = '';
  successMessage = '';
  
  // Skills and interests arrays
  skills: string[] = [];
  interests: string[] = [];
  
  // Suggestions
  showSkillSuggestions = false;
  showInterestSuggestions = false;
  filteredSkills: string[] = [];
  filteredInterests: string[] = [];
  
  // Common skills and interests
  allSkills = COMMON_SKILLS;
  allInterests = COMMON_INTERESTS;

  constructor(
    private formBuilder: FormBuilder,
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.registerForm = this.formBuilder.group({
      fullName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required],
      headline: [''],
      location: ['']
    }, {
      validators: this.passwordMatchValidator
    });
  }

  // Custom validator to check if passwords match
  passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password');
    const confirmPassword = control.get('confirmPassword');

    if (password?.value !== confirmPassword?.value) {
      confirmPassword?.setErrors({ mismatch: true });
      return { mismatch: true };
    }
    
    return null;
  }

  // Getter for easy access to form fields
  get f() { return this.registerForm.controls; }

  // Password strength calculation
  get passwordStrength(): string {
    const password = this.f['password'].value;
    if (!password) return 'weak';
    
    let strength = 0;
    if (password.length >= 6) strength++;
    if (password.match(/[a-z]+/)) strength++;
    if (password.match(/[A-Z]+/)) strength++;
    if (password.match(/[0-9]+/)) strength++;
    if (password.match(/[$@#&!]+/)) strength++;

    if (strength <= 2) return 'weak';
    if (strength <= 4) return 'medium';
    return 'strong';
  }

  get passwordStrengthText(): string {
    switch(this.passwordStrength) {
      case 'weak': return 'Weak';
      case 'medium': return 'Medium';
      case 'strong': return 'Strong';
      default: return 'Weak';
    }
  }

  // Skills methods
  addSkill(skill: string): void {
    const trimmedSkill = skill.trim();
    if (trimmedSkill && !this.skills.includes(trimmedSkill)) {
      this.skills.push(trimmedSkill);
    }
    this.showSkillSuggestions = false;
  }

  removeSkill(skill: string): void {
    this.skills = this.skills.filter(s => s !== skill);
  }

  onSkillInput(event: any, value: string): void {
    const input = value.toLowerCase();
    if (input.length > 0) {
      this.filteredSkills = this.allSkills.filter(skill => 
        skill.toLowerCase().includes(input) && !this.skills.includes(skill)
      ).slice(0, 5);
      this.showSkillSuggestions = this.filteredSkills.length > 0;
    } else {
      this.showSkillSuggestions = false;
    }
  }

  // Interests methods
  addInterest(interest: string): void {
    const trimmedInterest = interest.trim();
    if (trimmedInterest && !this.interests.includes(trimmedInterest)) {
      this.interests.push(trimmedInterest);
    }
    this.showInterestSuggestions = false;
  }

  removeInterest(interest: string): void {
    this.interests = this.interests.filter(i => i !== interest);
  }

  onInterestInput(event: any, value: string): void {
    const input = value.toLowerCase();
    if (input.length > 0) {
      this.filteredInterests = this.allInterests.filter(interest => 
        interest.toLowerCase().includes(input) && !this.interests.includes(interest)
      ).slice(0, 5);
      this.showInterestSuggestions = this.filteredInterests.length > 0;
    } else {
      this.showInterestSuggestions = false;
    }
  }

  onSubmit(): void {
    this.submitted = true;
    this.errorMessage = '';
    this.successMessage = '';

    // Stop if form is invalid
    if (this.registerForm.invalid) {
      return;
    }

    this.loading = true;

    const { confirmPassword, ...registerData } = this.registerForm.value;
    
    // Add skills and interests to registration data
    const finalData = {
      ...registerData,
      skills: this.skills,
      interests: this.interests
    };

    this.authService.register(finalData).subscribe({
      next: () => {
        this.successMessage = 'Registration successful! Redirecting to login...';
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 2000);
      },
      error: (error) => {
        this.errorMessage = error.error?.message || 'Registration failed. Please try again.';
        this.loading = false;
      }
    });
  }
}