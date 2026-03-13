import { GlobalErrorHandler } from './global-error-handler';

describe('GlobalErrorHandler', () => {
  it('should log Error objects to console.error', () => {
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {});
    const handler = new GlobalErrorHandler();
    const error = new Error('test error');
    handler.handleError(error);
    expect(spy).toHaveBeenCalledWith('[GlobalErrorHandler]', error);
    spy.mockRestore();
  });

  it('should log string errors to console.error', () => {
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {});
    const handler = new GlobalErrorHandler();
    handler.handleError('something went wrong');
    expect(spy).toHaveBeenCalledWith('[GlobalErrorHandler]', 'something went wrong');
    spy.mockRestore();
  });
});
