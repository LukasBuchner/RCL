import { createApolloClient } from "./createApolloClient";
import { defaultSettings } from "../types/settings";

// Create default Apollo client with default settings
// This will be replaced when settings are loaded
const client = createApolloClient(defaultSettings.graphql);

export default client;
