import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

const GAME_NAME_RE = /^[\p{L}\p{N} ]+$/u;
const TAG_LINE_RE = /^[A-Za-z0-9]+$/;

/**
 * gameName: 3–16 caracteres, permite Unicode (letras/dígitos) e espaços.
 * O trim é aplicado; vazio é tratado pelo `Validators.required`.
 */
export function gameNameValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '').trim();
    if (!value) {
      return null;
    }
    if (value.length < 3 || value.length > 16) {
      return { gameNameLength: { min: 3, max: 16, actual: value.length } };
    }
    if (!GAME_NAME_RE.test(value)) {
      return { gameNamePattern: true };
    }
    return null;
  };
}

/**
 * tagLine: 2–5 caracteres (aceita legado de 2), alfanumérico.
 */
export function tagLineValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '').trim();
    if (!value) {
      return null;
    }
    if (value.length < 2 || value.length > 5) {
      return { tagLineLength: { min: 2, max: 5, actual: value.length } };
    }
    if (!TAG_LINE_RE.test(value)) {
      return { tagLinePattern: true };
    }
    return null;
  };
}
