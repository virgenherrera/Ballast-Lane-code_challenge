import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-form-field',
  standalone: true,
  templateUrl: './form-field.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FormFieldComponent {
  label = input.required<string>();
  fieldId = input.required<string>();
  errorMessage = input<string | null>(null);
}
