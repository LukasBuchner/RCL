import React, { ReactElement } from "react";
import { render, RenderOptions } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import { MockedProvider, MockedResponse } from "@apollo/client/testing";
import { ReactFlowProvider } from "@xyflow/react";

interface AllTheProvidersProps {
  children: React.ReactNode;
  mocks?: MockedResponse[];
}

// Component needs to be defined in the same file as the custom render function
// eslint-disable-next-line react-refresh/only-export-components
const AllTheProviders = ({ children, mocks = [] }: AllTheProvidersProps) => {
  return (
    <MockedProvider mocks={mocks}>
      <ReactFlowProvider>
        <BrowserRouter>{children}</BrowserRouter>
      </ReactFlowProvider>
    </MockedProvider>
  );
};

const customRender = (
  ui: ReactElement,
  options?: Omit<RenderOptions, "wrapper"> & { mocks?: MockedResponse[] },
) => {
  const { mocks, ...renderOptions } = options || {};
  return render(ui, {
    wrapper: ({ children }) => (
      <AllTheProviders mocks={mocks}>{children}</AllTheProviders>
    ),
    ...renderOptions,
  });
};

// eslint-disable-next-line react-refresh/only-export-components
export * from "@testing-library/react";
export { customRender as render };
