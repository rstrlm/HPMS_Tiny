import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import { getBranding, type BrandingResponse } from "../api/branding";

type BrandingState = {
  companyName: string;
  tagline: string;
  isLoaded: boolean;
};

const defaults: BrandingState = {
  companyName: import.meta.env.VITE_APP_NAME || "PMS",
  tagline: "",
  isLoaded: false
};

const BrandingContext = createContext<BrandingState>(defaults);

export function BrandingProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<BrandingState>(defaults);

  useEffect(() => {
    getBranding()
      .then((data: BrandingResponse) => {
        setState({
          companyName: data.companyName,
          tagline: data.tagline,
          isLoaded: true
        });
        document.title = `${data.companyName} PMS`;
      })
      .catch(() => {
        // Fall back to env var or default
        setState((s) => ({ ...s, isLoaded: true }));
      });
  }, []);

  return (
    <BrandingContext.Provider value={state}>{children}</BrandingContext.Provider>
  );
}

export function useBranding() {
  return useContext(BrandingContext);
}
