import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormArray } from '@angular/forms';
import { ActivatedRoute, RouterModule, Router } from '@angular/router';
import { IdeaService, CreateIdeaRequest } from '../../../core/services/idea.service';

@Component({
  selector: 'app-idea-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './idea-form.component.html',
  styleUrls: ['./idea-form.component.scss']
})
export class IdeaFormComponent implements OnInit {
  ideaForm!: FormGroup;
  loading = false;
  submitting = false;
  error = '';
  isEditMode = false;
  ideaId: string = '';

  industries: string[] = [
    'AI/ML', 'EdTech', 'FinTech', 'HealthTech', 'SaaS', 
    'E-commerce', 'Social Media', 'Gaming', 'CleanTech', 'Other'
  ];
  
  stages: string[] = ['Idea', 'MVP', 'Prototype', 'Launched'];
  commitments: string[] = ['Full-time', 'Part-time', 'Weekends', 'Flexible'];

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private ideaService: IdeaService
  ) {}

  ngOnInit(): void {
    this.initForm();
    this.checkEditMode();
  }

  initForm(): void {
    this.ideaForm = this.fb.group({
      title: ['', Validators.required],
      description: ['', Validators.required],
      problemStatement: [''],
      solution: [''],
      industry: [''],
      stage: ['Idea'],
      lookingForSkills: this.fb.array([]),
      rolesNeeded: this.fb.array([])
    });

    // Add one empty role by default
    this.addRole();
  }

  checkEditMode(): void {
    this.ideaId = this.route.snapshot.paramMap.get('id') || '';
    if (this.ideaId) {
      this.isEditMode = true;
      this.loadIdeaForEdit();
    }
  }

  loadIdeaForEdit(): void {
    this.loading = true;
    this.ideaService.getIdeaById(this.ideaId).subscribe({
      next: (idea) => {
        this.ideaForm.patchValue({
          title: idea.title,
          description: idea.description,
          problemStatement: idea.problemStatement,
          solution: idea.solution,
          industry: idea.industry,
          stage: idea.stage
        });

        // Clear existing arrays
        this.lookingForSkills.clear();
        this.rolesNeeded.clear();

        // Add looking for skills
        if (idea.lookingForSkills?.length) {
          idea.lookingForSkills.forEach(skill => {
            this.lookingForSkills.push(this.fb.control(skill));
          });
        }

        // Add roles
        if (idea.rolesNeeded?.length) {
          idea.rolesNeeded.forEach(role => {
            const roleGroup = this.fb.group({
              role: [role.role, Validators.required],
              skills: [role.skills?.join(', ') || ''],
              commitment: [role.commitment || 'Part-time']
            });
            this.rolesNeeded.push(roleGroup);
          });
        }

        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading idea:', err);
        this.error = 'Failed to load idea. Please try again.';
        this.loading = false;
      }
    });
  }

  // Looking For Skills methods
  get lookingForSkills(): FormArray {
    return this.ideaForm.get('lookingForSkills') as FormArray;
  }

  addSkill(skillInput: HTMLInputElement): void {
    const skill = skillInput.value.trim();
    if (skill) {
      this.lookingForSkills.push(this.fb.control(skill));
      skillInput.value = '';
    }
  }

  removeSkill(index: number): void {
    this.lookingForSkills.removeAt(index);
  }

  // Roles methods
  get rolesNeeded(): FormArray {
    return this.ideaForm.get('rolesNeeded') as FormArray;
  }

  addRole(): void {
    const roleGroup = this.fb.group({
      role: ['', Validators.required],
      skills: [''],
      commitment: ['Part-time']
    });
    this.rolesNeeded.push(roleGroup);
  }

  removeRole(index: number): void {
    this.rolesNeeded.removeAt(index);
  }

  onSubmit(): void {
    if (this.ideaForm.invalid) {
      // Mark all fields as touched to show validation errors
      Object.keys(this.ideaForm.controls).forEach(key => {
        this.ideaForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.submitting = true;
    this.error = '';

    const formValue = this.ideaForm.value;
    
    // Process roles: convert comma-separated skills to array
    const rolesNeeded = formValue.rolesNeeded.map((role: any) => ({
      role: role.role,
      skills: role.skills ? role.skills.split(',').map((s: string) => s.trim()) : [],
      commitment: role.commitment
    }));

    const ideaData: CreateIdeaRequest = {
      title: formValue.title,
      description: formValue.description,
      problemStatement: formValue.problemStatement,
      solution: formValue.solution,
      industry: formValue.industry,
      stage: formValue.stage,
      lookingForSkills: formValue.lookingForSkills,
      rolesNeeded: rolesNeeded
    };

    const request = this.isEditMode
      ? this.ideaService.updateIdea(this.ideaId, ideaData)
      : this.ideaService.createIdea(ideaData);

    request.subscribe({
      next: () => {
        this.router.navigate(['/ideas']);
      },
      error: (err) => {
        console.error('Error saving idea:', err);
        this.error = 'Failed to save idea. Please try again.';
        this.submitting = false;
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/ideas']);
  }
}