import { CodegenConfig } from "@graphql-codegen/cli";

const config: CodegenConfig = {
  overwrite: true,
  schema: "../Backend/GraphQLServer/schema.graphql",
  documents: "src/graphql/*.graphql", // Include .ts and .tsx files
  generates: {
    // Output generated code to this directory
    "src/__generated__/": {
      preset: "client",
      plugins: [],
      presetConfig: {
        gqlTagName: "gql",
        fragmentMasking: false,
      },
    },
    // Generate a schema introspection file (optional)
    "./graphql.schema.json": {
      plugins: ["introspection"],
    },
  },
};

export default config;
