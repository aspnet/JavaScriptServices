import { fetch, addTask } from 'domain-task';
import { typeName, isActionType, Action, Reducer } from 'redux-typed';
import { ActionCreator } from './';

// -----------------
// STATE - This defines the type of data maintained in the Redux store.

export interface GenresListState {
    genres: Genre[];
    isLoaded: boolean;
}

export interface Genre {
    GenreId: string;
    Name: string;
}

// -----------------
// ACTIONS - These are serializable (hence replayable) descriptions of state transitions.
// They do not themselves have any side-effects; they just describe something that is going to happen.
// Use @typeName and isActionType for type detection that works even after serialization/deserialization.

@typeName("RECEIVE_GENRES_LIST")
class ReceiveGenresList extends Action {
    constructor(public genres: Genre[]) {
        super();
    }
}

// ----------------
// ACTION CREATORS - These are functions exposed to UI components that will trigger a state transition.
// They don't directly mutate state, but they can have external side-effects (such as loading data).

export const actionCreators = {
    requestGenresList: (): ActionCreator => (dispatch, getState) => {
        if (!getState().genreList.isLoaded) {
            let fetchTask = fetch('/api/genres')
                .then(results => results.json())
                .then(genres => dispatch(new ReceiveGenresList(genres)));
            addTask(fetchTask); // Ensure server-side prerendering waits for this to complete
        }
    }
};

// ----------------
// REDUCER - For a given state and action, returns the new state. To support time travel, this must not mutate the old state.
// For unrecognized actions, must return the existing state (or default initial state if none was supplied).

export const reducer: Reducer<GenresListState> = (state, action) => {
    if (isActionType(action, ReceiveGenresList)) {
        return { genres: action.genres, isLoaded: true };
    } else {
        return state || { genres: [], isLoaded: false };
    }
};
