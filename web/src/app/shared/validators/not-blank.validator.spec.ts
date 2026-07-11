import { FormControl } from '@angular/forms';

import { notBlankValidator } from './not-blank.validator';

describe('notBlankValidator', () => {
  it('notBlankValidator_WithEmptyString_ReturnsError', () => {
    const control = new FormControl('');

    const result = notBlankValidator()(control);

    expect(result).toEqual({ notBlank: true });
  });

  it('notBlankValidator_WithSpacesOnly_ReturnsError', () => {
    const control = new FormControl('    ');

    const result = notBlankValidator()(control);

    expect(result).toEqual({ notBlank: true });
  });

  it('notBlankValidator_WithNbspOnly_ReturnsError', () => {
    const control = new FormControl('   ');

    const result = notBlankValidator()(control);

    expect(result).toEqual({ notBlank: true });
  });

  it('notBlankValidator_WithValidText_ReturnsNull', () => {
    const control = new FormControl('Buy groceries');

    const result = notBlankValidator()(control);

    expect(result).toBeNull();
  });
});
