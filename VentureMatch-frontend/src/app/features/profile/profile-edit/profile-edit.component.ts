import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormArray } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { UserService } from '../../../core/services/user.service';

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
  selector: 'app-profile-edit',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './profile-edit.component.html',
  styleUrls: ['./profile-edit.component.scss']
})
export class ProfileEditComponent implements OnInit {
  profileForm!: FormGroup;
  loading = false;
  errorMessage = '';
  successMessage = '';
  
  // Skills and interests
  allSkills = COMMON_SKILLS;
  allInterests = COMMON_INTERESTS;
  
  // Suggestions
  showSkillSuggestions = false;
  showInterestSuggestions = false;
  filteredSkills: string[] = [];
  filteredInterests: string[] = [];
  
  // Current skill/interest input
  currentSkill = '';
  currentInterest = '';

  constructor(
    private formBuilder: FormBuilder,
    private router: Router,
    private authService: AuthService,
    private userService: UserService
  ) {}

  ngOnInit(): void {
    this.initForm();
    this.loadUserProfile();
  }

  initForm(): void {
    this.profileForm = this.formBuilder.group({
      fullName: ['', Validators.required],
      headline: [''],
      bio: [''],
      location: [''],
      avatarUrl: [''],
      skills: this.formBuilder.array([]),
      interests: this.formBuilder.array([]),
      availability: ['Part-time'],
      lookingFor: this.formBuilder.array([]),
      githubUrl: [''],
      linkedinUrl: [''],
      portfolioUrl: ['']
    });
  }

  loadUserProfile(): void {
    this.loading = true;
    this.userService.getProfile().subscribe({
      next: (user: any) => {
        console.log('✅ User profile loaded:', user);
        
        // Patch basic info
        this.profileForm.patchValue({
          fullName: user.fullName || '',
          headline: user.headline || '',
          bio: user.bio || '',
          location: user.location || '',
          avatarUrl: user.avatarUrl || '',
          availability: user.availability || 'Part-time',
          githubUrl: user.githubUrl || '',
          linkedinUrl: user.linkedinUrl || '',
          portfolioUrl: user.portfolioUrl || ''
        });

        // Clear existing arrays
        this.clearFormArray('skills');
        this.clearFormArray('interests');
        this.clearFormArray('lookingFor');

        // Add skills
        if (user.skills && user.skills.length) {
          user.skills.forEach((skill: string) => {
            this.addSkill(skill);
          });
        }

        // Add interests
        if (user.interests && user.interests.length) {
          user.interests.forEach((interest: string) => {
            this.addInterest(interest);
          });
        }

        // Add lookingFor
        if (user.lookingFor && user.lookingFor.length) {
          user.lookingFor.forEach((item: string) => {
            this.addLookingFor(item);
          });
        }

        this.loading = false;
      },
      error: (error) => {
        console.error('❌ Error loading profile:', error);
        this.errorMessage = 'Failed to load profile. Please try again.';
        this.loading = false;
      }
    });
  }

  // Form array helpers
  get skillsArray(): FormArray {
    return this.profileForm.get('skills') as FormArray;
  }

  get interestsArray(): FormArray {
    return this.profileForm.get('interests') as FormArray;
  }

  get lookingForArray(): FormArray {
    return this.profileForm.get('lookingFor') as FormArray;
  }

  clearFormArray(arrayName: string): void {
    const formArray = this.profileForm.get(arrayName) as FormArray;
    while (formArray.length) {
      formArray.removeAt(0);
    }
  }

  // Skills methods
  addSkill(skill?: string): void {
    const skillToAdd = skill || this.currentSkill;
    if (skillToAdd && skillToAdd.trim()) {
      const trimmed = skillToAdd.trim();
      // Check if already exists
      const exists = this.skillsArray.controls.some(
        control => control.value.toLowerCase() === trimmed.toLowerCase()
      );
      
      if (!exists) {
        this.skillsArray.push(this.formBuilder.control(trimmed));
      }
      
      this.currentSkill = '';
      this.showSkillSuggestions = false;
    }
  }

  removeSkill(index: number): void {
    this.skillsArray.removeAt(index);
  }

  onSkillInput(event: any): void {
    const value = event.target.value;
    this.currentSkill = value;
    
    if (value.length > 0) {
      const existingSkills = this.skillsArray.controls.map(c => c.value.toLowerCase());
      this.filteredSkills = this.allSkills
        .filter(skill => 
          skill.toLowerCase().includes(value.toLowerCase()) &&
          !existingSkills.includes(skill.toLowerCase())
        )
        .slice(0, 5);
      this.showSkillSuggestions = this.filteredSkills.length > 0;
    } else {
      this.showSkillSuggestions = false;
    }
  }

  selectSkill(skill: string): void {
    this.addSkill(skill);
    this.currentSkill = '';
    this.showSkillSuggestions = false;
  }

  // Interests methods
  addInterest(interest?: string): void {
    const interestToAdd = interest || this.currentInterest;
    if (interestToAdd && interestToAdd.trim()) {
      const trimmed = interestToAdd.trim();
      // Check if already exists
      const exists = this.interestsArray.controls.some(
        control => control.value.toLowerCase() === trimmed.toLowerCase()
      );
      
      if (!exists) {
        this.interestsArray.push(this.formBuilder.control(trimmed));
      }
      
      this.currentInterest = '';
      this.showInterestSuggestions = false;
    }
  }

  removeInterest(index: number): void {
    this.interestsArray.removeAt(index);
  }

  onInterestInput(event: any): void {
    const value = event.target.value;
    this.currentInterest = value;
    
    if (value.length > 0) {
      const existingInterests = this.interestsArray.controls.map(c => c.value.toLowerCase());
      this.filteredInterests = this.allInterests
        .filter(interest => 
          interest.toLowerCase().includes(value.toLowerCase()) &&
          !existingInterests.includes(interest.toLowerCase())
        )
        .slice(0, 5);
      this.showInterestSuggestions = this.filteredInterests.length > 0;
    } else {
      this.showInterestSuggestions = false;
    }
  }

  selectInterest(interest: string): void {
    this.addInterest(interest);
    this.currentInterest = '';
    this.showInterestSuggestions = false;
  }

  // Looking For methods
  addLookingFor(item?: string, input?: string): void {
    const itemToAdd = item || input;
    if (itemToAdd && itemToAdd.trim()) {
      this.lookingForArray.push(this.formBuilder.control(itemToAdd.trim()));
    }
  }

  removeLookingFor(index: number): void {
    this.lookingForArray.removeAt(index);
  }

  onSubmit(): void {
    if (this.profileForm.invalid) {
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    const formData = {
      fullName: this.profileForm.get('fullName')?.value,
      headline: this.profileForm.get('headline')?.value,
      bio: this.profileForm.get('bio')?.value,
      location: this.profileForm.get('location')?.value,
      avatarUrl: this.profileForm.get('avatarUrl')?.value,
      skills: this.skillsArray.controls.map(c => c.value),
      interests: this.interestsArray.controls.map(c => c.value),
      availability: this.profileForm.get('availability')?.value,
      lookingFor: this.lookingForArray.controls.map(c => c.value),
      githubUrl: this.profileForm.get('githubUrl')?.value,
      linkedinUrl: this.profileForm.get('linkedinUrl')?.value,
      portfolioUrl: this.profileForm.get('portfolioUrl')?.value
    };

    console.log('📡 Updating profile:', formData);

    this.userService.updateProfile(formData).subscribe({
      next: (response) => {
        console.log('✅ Profile updated:', response);
        this.successMessage = 'Profile updated successfully!';
        this.loading = false;
        
        // Redirect after 2 seconds
        setTimeout(() => {
          this.router.navigate(['/profile', response.id]);
        }, 2000);
      },
      error: (error) => {
        console.error('❌ Error updating profile:', error);
        this.errorMessage = error.error?.message || 'Failed to update profile. Please try again.';
        this.loading = false;
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/profile', this.authService.getCurrentUserId()]);
  }
}