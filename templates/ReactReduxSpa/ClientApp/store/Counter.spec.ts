import 'jest';
import { reducer } from './Counter';

describe('Coutner reducer', () => {
    it('should handle initial state', () => {
        expect(reducer(undefined, {} as any)).toEqual({ count: 0 });
    });

    it('should handle INCREMENT_COUNT', () => {
        expect(reducer(
            { count: 0 },
            { type: 'INCREMENT_COUNT' }))
        .toEqual({ count: 1 });
    });

    it('should handle DECREMENT_COUNT', () => {
        expect(reducer(
            { count: 1 },
            { type: 'DECREMENT_COUNT' }))
            .toEqual({ count: 0 });
    });
});