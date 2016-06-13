// Constant defining the action type
const INCREMENT_COUNT = 'INCREMENT_COUNT';

// Action creator
export const incrementCount = () => ({type: INCREMENT_COUNT});

const initialState = {
  count: 0
};

// The reducer that changes the state based on the action type
export default (state = initialState, action) => {
  if (action.type === INCREMENT_COUNT) {
    return { count: state.count +1 };
  }

  return state;
}
