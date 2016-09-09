import 'angular2-universal-polyfills';
import { assert } from 'chai';
import { Counter } from '../components/counter/counter';

describe('Counter component', () => {
    it('should start with value 0', () => {
        const instance = new Counter();
        assert.equal(instance.currentCount, 0);
    });

    it('should increment the counter by 1 when requested', () => {
        const instance = new Counter();
        instance.incrementCounter();
        assert.equal(instance.currentCount, 1);
        instance.incrementCounter();
        assert.equal(instance.currentCount, 2);
    });
});
