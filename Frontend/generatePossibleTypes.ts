import fetch from "cross-fetch";
import * as fs from "fs";

const YOUR_API_HOST = process.env.YOUR_API_HOST || "http://localhost:5095";

async function generatePossibleTypes() {
  const response = await fetch(`${YOUR_API_HOST}/graphql`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      query: `
        {
          __schema {
            types {
              kind
              name
              possibleTypes {
                name
              }
            }
          }
        }
      `,
      variables: {},
    }),
  });

  interface IntrospectionType {
    kind: string;
    name: string;
    possibleTypes?: { name: string }[];
  }

  const json = await response.json();
  const { types } = json.data.__schema;

  const possibleTypes: Record<string, string[]> = {};

  (types as IntrospectionType[]).forEach((supertype) => {
    if (supertype.possibleTypes) {
      possibleTypes[supertype.name] = supertype.possibleTypes.map(
        (subtype) => subtype.name,
      );
    }
  });

  fs.writeFileSync(
    "./possibleTypes.json",
    JSON.stringify(possibleTypes, null, 2),
  );
  console.log(
    "Fragment types successfully extracted and written to possibleTypes.json",
  );
}

generatePossibleTypes().catch((err) => {
  console.error("Error generating possibleTypes:", err);
  process.exit(1);
});
