import { DecimalPipe } from '@angular/common';
import { EaPowerValuePipe } from './ea-power-value-pipe';

describe('EaPowerValuePipe', () => {
  const pipe: EaPowerValuePipe = new EaPowerValuePipe(new DecimalPipe('en'));
  it('should be created', () => {
    expect(pipe).toBeTruthy();
  });

  it('should output value and unit', () => {
    const result: string = pipe.transform(10);
    expect(result).toBe('10 kW');
  });

  it('should format according to digits info', () => {
    const result: string = pipe.transform(10, '1.1-1');
    expect(result).toBe('10.0 kW');
  });

  it('should convert to target unit', () => {
    const result: string = pipe.transform(10, '', 'W');
    expect(result).toBe('10000 W');
  });

  it('should convert from source unit', () => {
    const result: string = pipe.transform(1, '', '', 'MW');
    expect(result).toBe('1000 kW');
  });

  it('should convert from source unit to target unit', () => {
    const result: string = pipe.transform(1, '', 'W', 'MW');
    expect(result).toBe('1000000 W');
  });

  it('should convert automatically to W when suitable', () => {
    const result: string = pipe.transform(0.01);
    expect(result).toBe('10 W');
  });

  it('should convert automatically to MW when suitable', () => {
    const result: string = pipe.transform(10000);
    expect(result).toBe('10 MW');
  });

  it('should convert automatically to GW when suitable', () => {
    const result: string = pipe.transform(10000000);
    expect(result).toBe('10 GW');
  });
});
