import type { ApiError } from './api-error-mapper';
import { mapApiErrorToFieldErrors } from './api-error-mapper';

describe('mapApiErrorToFieldErrors', () => {
  it('mapApiErrorToFieldErrors_WithSingleFieldError_MapsCorrectly', () => {
    const error: ApiError = {
      status: 400,
      error: 'VALIDATION_ERROR',
      message: 'Validation failed',
      details: [{ field: 'title', issue: 'title required' }],
    };

    const result = mapApiErrorToFieldErrors(error);

    expect(result).toEqual({ title: 'title required' });
  });

  it('mapApiErrorToFieldErrors_WithMultipleFieldErrors_MapsAllFields', () => {
    const error: ApiError = {
      status: 400,
      error: 'VALIDATION_ERROR',
      message: 'Validation failed',
      details: [
        { field: 'title', issue: 'title required' },
        { field: 'dueDate', issue: 'must be future' },
      ],
    };

    const result = mapApiErrorToFieldErrors(error);

    expect(result).toEqual({
      title: 'title required',
      dueDate: 'must be future',
    });
  });

  it('mapApiErrorToFieldErrors_WithEmptyDetails_ReturnsEmptyMap', () => {
    const error: ApiError = {
      status: 400,
      error: 'VALIDATION_ERROR',
      message: 'Validation failed',
      details: [],
    };

    const result = mapApiErrorToFieldErrors(error);

    expect(result).toEqual({});
  });
});
