export interface GraphQLSettings {
  httpEndpoint: string;
  wsEndpoint: string;
  enableSubscriptions: boolean;
  timeout: number;
}

export interface AppearanceSettings {
  theme: "light" | "dark" | "auto";
  animationSpeed: number;
  reduceMotion: boolean;
  showEdgeLabels: boolean;
}

export interface GeneralSettings {
  enableNotifications: boolean;
  language: "en" | "de" | "pl" | "es";
}

export interface AppSettings {
  graphql: GraphQLSettings;
  appearance: AppearanceSettings;
  general: GeneralSettings;
}

export const defaultSettings: AppSettings = {
  graphql: {
    httpEndpoint: "http://localhost:5095/graphql",
    wsEndpoint: "ws://localhost:5095/graphql",
    enableSubscriptions: true,
    timeout: 30000,
  },
  appearance: {
    theme: "light",
    animationSpeed: 1,
    reduceMotion: false,
    showEdgeLabels: true,
  },
  general: {
    enableNotifications: true,
    language: "en",
  },
};
