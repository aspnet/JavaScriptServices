import * as React from 'react';
import { Provider } from 'react-redux';
import { renderToString } from 'react-dom/server';
import { match, RouterContext } from 'react-router';
import createMemoryHistory from 'history/lib/createMemoryHistory';
import { createServerRenderer, RenderResult } from 'aspnet-prerendering';
import routes from './routes';
import configureStore from './configureStore';
import cookieUtil from 'cookie';
import cookie from 'react-cookie';

function plugInCookiesFromDotNet(cookieData: { key: string, value: string }[], res) {
    const formattedData = {};
    cookieData.forEach(keyValuePair => {
        formattedData[keyValuePair.key] = keyValuePair.value;
    });
    cookie.plugToRequest({ cookies: formattedData }, res);
}

export default createServerRenderer(params => {
    return new Promise<RenderResult>((resolve, reject) => {
        const cookiesModifiedOnServer = {};
        if (params.data.cookies) {
            // If we received some cookie data, use that to prepopulate 'react-cookie'
            plugInCookiesFromDotNet(params.data.cookies, {
                // Also track any cookies written on the server
                cookie: (name, val) => { cookiesModifiedOnServer[name] = val; }
            })
        }

        // Match the incoming request against the list of client-side routes
        match({ routes, location: params.location }, (error, redirectLocation, renderProps: any) => {
            if (error) {
                throw error;
            }

            // If there's a redirection, just send this information back to the host application
            if (redirectLocation) {
                resolve({ redirectUrl: redirectLocation.pathname });
                return;
            }

            // If it didn't match any route, renderProps will be undefined
            if (!renderProps) {
                throw new Error(`The location '${ params.url }' doesn't match any route configured in react-router.`);
            }

            // Build an instance of the application
            const store = configureStore();
            const app = (
                <Provider store={ store }>
                    <RouterContext {...renderProps} />
                </Provider>
            );

            // Perform an initial render that will cause any async tasks (e.g., data access) to begin
            renderToString(app);

            // Once the tasks are done, we can perform the final render
            // We also send the redux store state, so the client can continue execution where the server left off
            params.domainTasks.then(() => {
                resolve({
                    html: renderToString(app),
                    globals: {
                        initialReduxState: store.getState(),

                        // Send any cookies written during server-side prerendering to the client.
                        // WARNING: Do not pass any security-sensitive cookies this way, because they will become
                        // readable in the HTML source. If your goal is to use this approach to manage authentication
                        // cookies, then be sure *not* to use 'globals' to send them to the client - instead, invoke
                        // Microsoft.AspNetCore.SpaServices.Prerendering.Prerender directly from your .NET code and
                        // only send the 'html' part back to the client (or at least, not all of the 'globals').
                        cookieData: cookiesModifiedOnServer
                    }
                });
            }, reject); // Also propagate any errors back into the host application
        });
    });
});
