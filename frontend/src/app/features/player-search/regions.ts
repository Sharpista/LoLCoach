/** Plataformas LoL aceitas pelo backend (spec 001 / design.md). */
export interface RegionOption {
  value: string;
  label: string;
}

export const REGIONS: RegionOption[] = [
  { value: 'br1', label: 'Brasil (BR1)' },
  { value: 'eun1', label: 'Europa Nordic & East (EUN1)' },
  { value: 'euw1', label: 'Europa West (EUW1)' },
  { value: 'jp1', label: 'Japão (JP1)' },
  { value: 'kr', label: 'Coreia (KR)' },
  { value: 'la1', label: 'América Latina Norte (LA1)' },
  { value: 'la2', label: 'América Latina Sul (LA2)' },
  { value: 'na1', label: 'América do Norte (NA1)' },
  { value: 'oc1', label: 'Oceania (OC1)' },
  { value: 'ph2', label: 'Filipinas (PH2)' },
  { value: 'ru', label: 'Rússia (RU)' },
  { value: 'sg2', label: 'Singapura (SG2)' },
  { value: 'th2', label: 'Tailândia (TH2)' },
  { value: 'tr1', label: 'Turquia (TR1)' },
  { value: 'tw2', label: 'Taiwan (TW2)' },
  { value: 'vn2', label: 'Vietnã (VN2)' },
];
